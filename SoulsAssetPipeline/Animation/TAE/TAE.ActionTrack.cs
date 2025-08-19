using SoulsFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoulsAssetPipeline.Animation
{
    public partial class TAE : SoulsFile<TAE>
    {

        /// <summary>
        /// A track (row) with actions in it.
        /// </summary>
        public class ActionTrack
        {
            /// <summary>
            /// Unknown.
            /// </summary>
            public int TrackType { get; set; }

            /// <summary>
            /// Used for when it autogenerates tracks for old games that don't have them,
            /// specifically for DS Anim Studio lmao.
            /// </summary>
            public int AutogenTrack_Type = 0;
            public int AutogenTrack_Subtype = 0;

            public long AutogenTrack_CompositeType => (AutogenTrack_Type * 0x1_00_00_00_00) + AutogenTrack_Subtype;



            public ActionTrack()
            {

            }

            public ActionTrackDataStruct TrackData;

            public ActionTrack GetClone()
            {
                var g = new ActionTrack(TrackType, AutogenTrack_Type, AutogenTrack_Subtype);
                g.TrackData = TrackData;
                return g;
            }

            public enum ActionTrackDataType : long
            {
                TrackData0 = 0,
                TrackData16 = 16,
                ApplyToSpecificCutsceneEntity = 128,
                TrackData192 = 192,
            }

            public struct ActionTrackDataStruct
            {
                public ActionTrackDataType DataType;
                public enum EntityTypes : ushort
                {
                    Character = 0,
                    Object = 1,
                    MapPiece = 2,
                    DummyNode = 4,
                }
                public EntityTypes CutsceneEntityType;
                public short CutsceneEntityIDPart1;
                public short CutsceneEntityIDPart2;
                public sbyte Area;
                public sbyte Block;

                public string GetEntityNameString()
                {
                    string name = "c";
                    if (CutsceneEntityType == EntityTypes.Object)
                        name = "o";
                    else if (CutsceneEntityType == EntityTypes.MapPiece)
                        name = "m";
                    else if (CutsceneEntityType == EntityTypes.DummyNode)
                        name = "d";

                    if (CutsceneEntityIDPart1 >= 0)
                        name += $"{CutsceneEntityIDPart1:D4}_";
                    else
                        name += "????_";
                    
                    if (CutsceneEntityIDPart2 >= 0)
                        name += $"{CutsceneEntityIDPart2:D4}";
                    else
                        name += "????";

                    return name;
                }

                public string GetAreaNameString()
                {
                    return $"m{(Area >= 0 ? $"{Area:D2}" : "??")}_{(Block >= 0 ? $"{Block:D2}" : "??")}_??_??";
                }

                public override bool Equals(object obj)
                {
                    if (obj is ActionTrackDataStruct asStruct)
                    {
                        return asStruct.DataType == DataType
                               && asStruct.CutsceneEntityType == CutsceneEntityType
                               && asStruct.CutsceneEntityIDPart1 == CutsceneEntityIDPart1
                               && asStruct.CutsceneEntityIDPart2 == CutsceneEntityIDPart2
                               && asStruct.Area == Area
                               && asStruct.Block == Block;
                    }

                    return false;
                }

                public static bool operator ==(ActionTrackDataStruct a, ActionTrackDataStruct b)
                {
                    return a.Equals(b);
                }
                
                public static bool operator !=(ActionTrackDataStruct a, ActionTrackDataStruct b)
                {
                    return !a.Equals(b);
                }
            }

            internal List<int> indices;

            /// <summary>
            /// Creates a new empty ActionTrack with the given type.
            /// </summary>
            public ActionTrack(int actionType, int autogenTrackType, int autogenTrackSubtype)
            {
                TrackType = actionType;
                AutogenTrack_Type = autogenTrackType;
                AutogenTrack_Subtype = autogenTrackSubtype;
                indices = new List<int>();
            }

            internal ActionTrack(BinaryReaderEx br, List<long> eventHeaderOffsets, TAEFormat format)
            {
                long entryCount = br.ReadVarint();
                long valuesOffset = br.ReadVarint();
                long typeOffset = br.ReadVarint();
                if (format is not TAEFormat.DS1 && format is not TAEFormat.DESR)
                    br.AssertVarint(0);

                br.StepIn(typeOffset);
                {
                    TrackType = br.ReadInt32();
                    if (br.VarintLong)
                        br.AssertInt32(0);
                    if (format is TAEFormat.SOTFS)
                    {
                        br.AssertVarint(br.Position + (br.VarintLong ? 8 : 4));
                        br.AssertVarint(0);
                        br.AssertVarint(0);
                    }
                    else if (format is TAEFormat.DS3 or TAEFormat.SDT)
                    {
                        //ac6 heuristic
                        if (format is TAEFormat.SDT && br.GetVarint(br.Position) == 0)
                            br.AssertVarint(0);
                    }
                    else
                    {
                        TrackData.DataType = (ActionTrackDataType)TrackType;
                        long dataOffset = br.ReadVarint();
                        if (dataOffset != 0)
                        {
                            br.StepIn(dataOffset);
                            {
                                if (TrackData.DataType is ActionTrackDataType.ApplyToSpecificCutsceneEntity)
                                {
                                    TrackData.CutsceneEntityType = br.ReadEnum16<ActionTrackDataStruct.EntityTypes>();
                                    TrackData.CutsceneEntityIDPart1 = br.ReadInt16();
                                    TrackData.CutsceneEntityIDPart2 = br.ReadInt16();
                                    TrackData.Block = br.ReadSByte();
                                    TrackData.Area = br.ReadSByte();
                                    br.AssertInt32(0);
                                    br.AssertInt32(0);
                                }
                            }
                            br.StepOut();
                        }
                    }
                }
                br.StepOut();

                br.StepIn(valuesOffset);
                {
                    if (format == TAEFormat.SOTFS)
                        indices = br.ReadVarints((int)entryCount).Select(offset
                            => eventHeaderOffsets.FindIndex(headerOffset => headerOffset == offset)).ToList();
                    else
                        indices = br.ReadInt32s((int)entryCount).Select(offset
                            => eventHeaderOffsets.FindIndex(headerOffset => headerOffset == offset)).ToList();
                }
                br.StepOut();
            }

            internal void WriteHeader(BinaryWriterEx bw, int i, int j, TAEFormat format)
            {
                bw.WriteVarint(indices.Count);
                bw.ReserveVarint($"EventGroupValuesOffset{i}:{j}");
                bw.ReserveVarint($"EventGroupTypeOffset{i}:{j}");
                if (format is not TAEFormat.DS1 && format is not TAEFormat.DESR)
                    bw.WriteVarint(0);
            }

            internal void WriteData(BinaryWriterEx bw, int i, int j, List<long> eventHeaderOffsets, TAEFormat format)
            {
                bw.FillVarint($"EventGroupTypeOffset{i}:{j}", bw.Position);
                bw.WriteInt32(TrackType);
                if (bw.VarintLong)
                    bw.WriteInt32(0);

                if (format == TAEFormat.SOTFS)
                {
                    bw.WriteVarint(bw.Position + (bw.VarintLong ? 8 : 4));
                    bw.WriteVarint(0);
                    bw.WriteVarint(0);
                }
                else if (format == TAEFormat.DS3 || format == TAEFormat.SDT)
                {
                    bw.WriteVarint(0);
                }
                else
                {
                    bw.ReserveVarint("EventGroupDataOffset");
                    long dataStartPos = bw.Position;
                    
                    if (TrackData.DataType == ActionTrackDataType.ApplyToSpecificCutsceneEntity)
                    {
                        bw.WriteUInt16((ushort)(TrackData.CutsceneEntityType));
                        bw.WriteInt16(TrackData.CutsceneEntityIDPart1);
                        bw.WriteInt16(TrackData.CutsceneEntityIDPart2);
                        bw.WriteSByte(TrackData.Block);
                        bw.WriteSByte(TrackData.Area);
                        bw.WriteInt32(0);
                        bw.WriteInt32(0);
                    }

                    if (dataStartPos != bw.Position)
                    {
                        bw.FillVarint("EventGroupDataOffset", dataStartPos);
                    }
                    else
                    {
                        bw.FillVarint("EventGroupDataOffset", 0);
                    }


                    if ((int)TrackData.DataType != TrackType)
                    {
                        throw new InvalidDataException("TAE event group data is not for the correct type.");
                    }
                }

                bw.FillVarint($"EventGroupValuesOffset{i}:{j}", bw.Position);
                for (int k = 0; k < indices.Count; k++)
                {
                    if (format == TAEFormat.SOTFS)
                        bw.WriteVarint(eventHeaderOffsets[indices[k]]);
                    else
                        bw.WriteInt32((int)eventHeaderOffsets[indices[k]]);
                }

                if (format != TAEFormat.DS1)
                    bw.Pad(0x10);
            }
        }

    }
}
