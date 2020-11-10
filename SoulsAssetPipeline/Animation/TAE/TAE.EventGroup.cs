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
        /// A group of events in an animation with an associated EventType that does not necessarily match theirs.
        /// </summary>
        public class EventGroup
        {
            /// <summary>
            /// Unknown.
            /// </summary>
            public long GroupType { get; set; }

            public EventGroup()
            {

            }

            public EventGroupData GroupData { get; set; } = null;

            public enum EventGroupDataType : long
            {
                GroupData0 = 0,
                GroupData16 = 16,
                ApplyToSpecificCutsceneEntity = 128,
                GroupData192 = 192,
            }

            public class EventGroupData
            {
                internal virtual int GetGroupTypeThisIsFor() => -1;
                public virtual void ReadInner(BinaryReaderEx br)
                {

                }
                public virtual void WriteInner(BinaryWriterEx bw)
                {

                }
                public void Read(BinaryReaderEx br)
                {
                    long dataOffset = br.ReadVarint();
                    if (dataOffset != 0)
                        ReadInner(br);
                }
                public void Write(BinaryWriterEx bw)
                {
                    bw.ReserveVarint("EventGroupDataOffset");
                    long dataStartPos = bw.Position;
                    WriteInner(bw);
                    if (dataStartPos != bw.Position)
                    {
                        bw.FillVarint("EventGroupDataOffset", dataStartPos);
                    }
                    else
                    {
                        bw.FillVarint("EventGroupDataOffset", 0);
                    }
                }

                public class GroupData0 : EventGroupData
                {
                    internal override int GetGroupTypeThisIsFor() => 0;
                }

                public class GroupData16 : EventGroupData
                {
                    internal override int GetGroupTypeThisIsFor() => 16;
                }

                public class ApplyToSpecificCutsceneEntity : EventGroupData
                {

                    internal override int GetGroupTypeThisIsFor() => 128;

                    public enum EntityTypes : ushort
                    {
                        Character = 0,
                        Object = 1,
                        MapPiece = 2,
                        DummyNode = 4,
                    }
                    public EntityTypes CutsceneEntityType { get; set; } = EntityTypes.Character;
                    public short CutsceneEntityIDPart1 { get; set; } = 0;
                    public short CutsceneEntityIDPart2 { get; set; } = 0;
                    public sbyte Area { get; set; } = -1;
                    public sbyte Block { get; set; } = -1;
                    public override void ReadInner(BinaryReaderEx br)
                    {
                        CutsceneEntityType = br.ReadEnum16<EntityTypes>();
                        CutsceneEntityIDPart1 = br.ReadInt16();
                        CutsceneEntityIDPart2 = br.ReadInt16();
                        Block = br.ReadSByte();
                        Area = br.ReadSByte();
                        br.AssertInt32(0);
                        br.AssertInt32(0);
                    }
                    public override void WriteInner(BinaryWriterEx bw)
                    {
                        bw.WriteUInt16((ushort)CutsceneEntityType);
                        bw.WriteInt16(CutsceneEntityIDPart1);
                        bw.WriteInt16(CutsceneEntityIDPart2);
                        bw.WriteSByte(Block);
                        bw.WriteSByte(Area);
                        bw.WriteInt32(0);
                        bw.WriteInt32(0);
                    }
                }

                public class GroupData192 : EventGroupData
                {
                    internal override int GetGroupTypeThisIsFor() => 192;
                }
            }

            internal List<int> indices;

            /// <summary>
            /// Creates a new empty EventGroup with the given type.
            /// </summary>
            public EventGroup(long eventType)
            {
                GroupType = eventType;
                indices = new List<int>();
            }

            internal EventGroup(BinaryReaderEx br, List<long> eventHeaderOffsets, TAEFormat format)
            {
                long entryCount = br.ReadVarint();
                long valuesOffset = br.ReadVarint();
                long typeOffset = br.ReadVarint();
                if (format != TAEFormat.DS1)
                    br.AssertVarint(0);

                br.StepIn(typeOffset);
                {
                    GroupType = br.ReadVarint();
                    if (format == TAEFormat.SOTFS)
                    {
                        br.AssertVarint(br.Position + (br.VarintLong ? 8 : 4));
                        br.AssertVarint(0);
                        br.AssertVarint(0);
                    }
                    else if (format != TAEFormat.DS1)
                    {
                        br.AssertVarint(0);
                    }
                    else
                    {
                        var groupDataType = (EventGroupDataType)GroupType;

                        if (groupDataType == EventGroupDataType.GroupData0)
                            GroupData = new EventGroupData.GroupData0();

                        else if (groupDataType == EventGroupDataType.GroupData16)
                            GroupData = new EventGroupData.GroupData16();

                        else if (groupDataType == EventGroupDataType.ApplyToSpecificCutsceneEntity)
                            GroupData = new EventGroupData.ApplyToSpecificCutsceneEntity();

                        else if (groupDataType == EventGroupDataType.GroupData192)
                            GroupData = new EventGroupData.GroupData192();

                        GroupData.Read(br);
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
                if (format != TAEFormat.DS1)
                    bw.WriteVarint(0);
            }

            internal void WriteData(BinaryWriterEx bw, int i, int j, List<long> eventHeaderOffsets, TAEFormat format)
            {
                bw.FillVarint($"EventGroupTypeOffset{i}:{j}", bw.Position);
                bw.WriteVarint(GroupType);

                if (format == TAEFormat.SOTFS)
                {
                    bw.WriteVarint(bw.Position + (bw.VarintLong ? 8 : 4));
                    bw.WriteVarint(0);
                    bw.WriteVarint(0);
                }
                else if (format != TAEFormat.DS1)
                {
                    bw.WriteVarint(0);
                }
                else
                {
                    if (GroupData != null)
                    {
                        if (GroupData?.GetGroupTypeThisIsFor() != GroupType)
                        {
                            throw new InvalidDataException("TAE event group data is not for the correct type.");
                        }

                        GroupData.Write(bw);
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
