using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoulsAssetPipeline.Audio.Wwise
{
    public abstract partial class WwiseBlock
    {
        public class HIRC : WwiseBlock
        {
            public HIRC()
                : base("HIRC")
            {

            }

            public class WwiseObjectInfo
            {
                public enum ObjTypes : byte
                {
                    _CAkState = 1, // Unmapped
                    CAkSound = 2,
                    CAkAction = 3,
                    CAkEvent = 4,
                    CAkRanSeqCntr = 5,
                    CAkSwitchCntr = 6,
                    _CAkActorMixer = 7, // Unmapped
                    _CAkBus = 8, // Unmapped
                    CAkLayerCntr = 9,
                    _CAkMusicSegment = 10, // Unmapped
                    _CAkMusicTrack = 11, // Unmapped
                    _CAkMusicSwitchCntr = 12, // Unmapped
                    _CAkMusicRanSeqCntr = 13, // Unmapped
                    _CAkAttenuation = 14, // Unmapped
                    CAkDialogueEvent = 15,
                    _CAkFxCustom = 17, // Unmapped
                    _CAkAuxBus = 18, // Unmapped
                    _UnkType19 = 19, // Unmapped
                    _UnkType20 = 20, // Unmapped
                    _CAkAudioDevice = 21, // Unmapped
                    _CAkTimeModulator = 22, // Unmapped
                }
                public ObjTypes ObjectType;
                public int HircOffset;
                public int ObjectSize;
            }

            internal BinaryReaderEx objFetchBinaryReader;
            private Dictionary<uint, WwiseObjectInfo> wwObjectInfos = new Dictionary<uint, WwiseObjectInfo>();
            public IReadOnlyDictionary<uint, WwiseObjectInfo> ObjectInfos => wwObjectInfos;

            private Dictionary<uint, IWwiseObject> loadedObjects = new Dictionary<uint, IWwiseObject>();
            public IReadOnlyDictionary<uint, IWwiseObject> LoadedObjects => loadedObjects;

            private object _lock_LoadObjectDynamic = new object();

            public IWwiseObject LoadObjectDynamic(uint id)
            {
                IWwiseObject result = null;
                lock (_lock_LoadObjectDynamic)
                {
                    if (loadedObjects.ContainsKey(id))
                    {
                        result = loadedObjects[id];
                    }
                    else
                    {
                        if (!wwObjectInfos.ContainsKey(id))
                            return null;
                        var info = wwObjectInfos[id];

                        IWwiseObject t = null;

                        switch (info.ObjectType)
                        {
                            case WwiseObjectInfo.ObjTypes.CAkAction:
                                t = new WwiseObject.CAkAction();
                                break;
                            case WwiseObjectInfo.ObjTypes.CAkEvent:
                                t = new WwiseObject.CAkEvent();
                                break;
                            case WwiseObjectInfo.ObjTypes.CAkRanSeqCntr:
                                t = new WwiseObject.CAkRanSeqCntr();
                                break;
                            case WwiseObjectInfo.ObjTypes.CAkSound:
                                t = new WwiseObject.CAkSound();
                                break;
                            case WwiseObjectInfo.ObjTypes.CAkSwitchCntr:
                                t = new WwiseObject.CAkSwitchCntr();
                                break;
                            case WwiseObjectInfo.ObjTypes.CAkLayerCntr:
                                t = new WwiseObject.CAkLayerCntr();
                                break;
                            case WwiseObjectInfo.ObjTypes.CAkDialogueEvent:
                                t = new WwiseObject.CAkDialogueEvent();
                                break;
                            default:
                                throw new NotImplementedException();
                        }

                        objFetchBinaryReader.StepIn(info.HircOffset);
                        objFetchBinaryReader.AssertUInt32(id);
                        t.Read(objFetchBinaryReader, null);
                        loadedObjects[id] = t;
                        result = t;
                    }
                }
                return result;
            }

            public T LoadObject<T>(uint id)
                where T : class, IWwiseObject, new()
            {
                if (loadedObjects.ContainsKey(id) && loadedObjects[id] is T asT)
                {
                    return asT;
                }
                else
                {
                    if (!wwObjectInfos.ContainsKey(id))
                        return null;
                    var info = wwObjectInfos[id];
                    var t = new T();
                    objFetchBinaryReader.StepIn(info.HircOffset);
                    objFetchBinaryReader.AssertUInt32(id);
                    t.Read(objFetchBinaryReader, null);
                    loadedObjects.Add(id, t);
                    return t;
                }

            }

            public override void InnerRead(BinaryReaderEx br, int sectionLength)
            {
                var startPos = br.Position;
                objFetchBinaryReader = new BinaryReaderEx(false, br.GetBytes(br.Position, sectionLength));

                int numObjects = br.ReadInt32();
                wwObjectInfos.Clear();
                for (int i = 0; i < numObjects; i++)
                {
                    var objectType = (WwiseObjectInfo.ObjTypes)br.ReadByte();
                    int objectSize = br.ReadInt32();
                    var objectStart = br.Position;
                    int offset = (int)(objectStart - startPos);
                    uint id = br.ReadUInt32();
                    wwObjectInfos.Add(id, new WwiseObjectInfo()
                    {
                        ObjectType = objectType,
                        HircOffset = offset,
                        ObjectSize = objectSize
                    });
                    br.Position = (objectStart + objectSize);
                }
            }

            public override void InnerWrite(BinaryWriterEx bw)
            {
                throw new NotImplementedException();
            }
        }
    }
}
