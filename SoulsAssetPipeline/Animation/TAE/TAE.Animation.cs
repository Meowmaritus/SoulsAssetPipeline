using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoulsAssetPipeline.Animation
{
    public partial class TAE : SoulsFile<TAE>
    {

        /// <summary>
        /// Controls an individual animation.
        /// </summary>
        public class Animation
        {
            public enum AnimFileHeaderType : uint
            {
                /// <summary>
                /// Standard AnimFileHeader with three flags, one of which can import the motion from another animation.
                /// </summary>
                Standard = 0,

                /// <summary>
                /// AnimFileHeader that signifies that the animation fully imports the motion data and all actions from another animation.
                /// </summary>
                ImportOtherAnim = 1
            }

            public abstract class AnimFileHeader
            {
                /// <summary>
                /// Type of AnimFileHeader that this is.
                /// </summary>
                public AnimFileHeaderType Type { get; set; }
                internal abstract void ReadInner(BinaryReaderEx br, TAEFormat format);
                internal abstract void WriteInner(BinaryWriterEx bw, TAEFormat format);


                //internal Guid? TEST_GUID = Guid.NewGuid();


                internal void Write(BinaryWriterEx bw, TAEFormat format)
                {
                    bw.WriteVarint((int)Type);

                    if (IsNullHeader)
                    {
                        bw.WriteVarint(0);
                        
                    }
                    else
                    {
                        bw.ReserveVarint("AnimFileNameOffsetOffset");
                        
                        
                    }

                    if (format is TAEFormat.DES) // Not in DESR
                        bw.Pad(0x10);


                    


                    if (IsNullHeader)
                    {
                        return;
                    }


                    bw.FillVarint("AnimFileNameOffsetOffset", bw.Position);

                    bw.ReserveVarint("AnimFileNameOffset");


                    //if (AnimFileReference)
                    //{
                    //    bw.WriteInt32(ReferenceID);

                    //    bw.WriteBoolean(UnkReferenceFlag1);
                    //    bw.WriteBoolean(ReferenceIsTAEOnly);
                    //    bw.WriteBoolean(ReferenceIsHKXOnly);
                    //    bw.WriteBoolean(LoopByDefault);
                    //}
                    //else
                    //{
                    //    bw.WriteBoolean(UnkReferenceFlag1);
                    //    bw.WriteBoolean(ReferenceIsTAEOnly);
                    //    bw.WriteBoolean(ReferenceIsHKXOnly);
                    //    bw.WriteBoolean(LoopByDefault);

                    //    bw.WriteInt32(ReferenceID);
                    //}

                    WriteInner(bw, format);

                    if (!(format is TAEFormat.DES or TAEFormat.DS1 or TAEFormat.DESR))
                    {
                        bw.WriteVarint(0);
                        bw.WriteVarint(0);
                    }
                    else
                    {
                        if (format is TAEFormat.DESR)
                        {
                            bw.WriteInt32(0);

                            if (Type is AnimFileHeaderType.ImportOtherAnim)
                                bw.WriteInt32(0);
                        }
                        else
                        {
                            bw.WriteVarint(0);

                            if (Type is AnimFileHeaderType.ImportOtherAnim)
                                bw.WriteVarint(0);
                        }
                    }

                    bw.FillVarint("AnimFileNameOffset", bw.Position);
                    if (!string.IsNullOrWhiteSpace(AnimFileName))
                    {
                        bw.WriteUTF16(AnimFileName, true);

                        if (format is not TAEFormat.DS1)
                            bw.Pad(0x10);
                    }
                    else
                    {
                        // Null terminate immediately
                        bw.WriteInt16(0);
                    }

                    //TaeGuidTest
                    //if (TEST_GUID != null)
                    //{
                    //    bw.WriteASCII("GUID");
                    //    bw.WriteBytes(TEST_GUID.Value.ToByteArray());
                    //}
                }


                internal static AnimFileHeader Read(BinaryReaderEx br, TAEFormat format, long timesOffset)
                {
                    var miniHeaderType = br.ReadEnum32<AnimFileHeaderType>();

                    if (br.VarintLong)
                        br.AssertInt32(0);

                    var potentialFileNameOffsetOffset = br.GetNextPaddedOffsetAfterCurrentField(br.VarintSize, format == TAEFormat.DES ? 0x10 : 0);

                    // Offset being read as 32bit int to deal with bad data in the upper 32bits of the offsets in DESR
                    int actualFileNameOffsetOffset = br.AssertInt32((int)potentialFileNameOffsetOffset, 0);
                    if (br.VarintLong)
                        br.ReadInt32();

                    AnimFileHeader header = null;

                    if (actualFileNameOffsetOffset == 0)
                    {
                        if (miniHeaderType == AnimFileHeaderType.Standard)
                            header = new AnimFileHeader.Standard();
                        else if (miniHeaderType == AnimFileHeaderType.ImportOtherAnim)
                            header = new AnimFileHeader.ImportOtherAnim();
                        else
                            throw new NotImplementedException($"{nameof(AnimFileHeader)} type not implemented yet.");

                        header.IsNullHeader = true;
                    }
                    else
                    {
                        br.Position = actualFileNameOffsetOffset;

                        // Offset being read as 32bit int to deal with bad data in the upper 32bits of the offsets in DESR
                        int animFileNameOffset = br.ReadInt32();
                        if (br.VarintLong)
                            br.ReadInt32();



                        if (miniHeaderType == AnimFileHeaderType.Standard)
                            header = new AnimFileHeader.Standard();
                        else if (miniHeaderType == AnimFileHeaderType.ImportOtherAnim)
                            header = new AnimFileHeader.ImportOtherAnim();
                        else
                            throw new NotImplementedException($"{nameof(AnimFileHeader)} type not implemented yet.");

                        header.ReadInner(br, format);

                        if (!(format == TAEFormat.DES || format == TAEFormat.DS1 || format == TAEFormat.DESR))
                        {
                            br.AssertVarint(0);
                            br.AssertVarint(0);
                        }
                        else
                        {
                            // Check for end of file for certain DESR files where this struct is the very last thing
                            // and it does not have the padding in such case
                            if (br.Position < br.Length)
                            {
                                if (format == TAEFormat.DESR)
                                {
                                    br.AssertInt32(0);

                                    if (header.Type == AnimFileHeaderType.ImportOtherAnim)
                                        br.AssertInt32(0);
                                }
                                else
                                {
                                    br.AssertVarint(0);

                                    if (header.Type == AnimFileHeaderType.ImportOtherAnim)
                                        br.AssertVarint(0);
                                }
                            }

                        }

                        if (animFileNameOffset < br.Length && animFileNameOffset != timesOffset)
                        {
                            if (br.GetInt64(animFileNameOffset) != 1)
                            {
                                var floatCheck = br.GetSingle(animFileNameOffset);
                                if (!(floatCheck >= 0.016667f && floatCheck <= 100))
                                {
                                    header.AnimFileName = br.GetUTF16(animFileNameOffset);
                                }
                            }
                        }

                        header.AnimFileName = header.AnimFileName ?? "";

                        // When Reference is false, there's always a filename.
                        // When true, there's usually not, but sometimes there is, and I cannot figure out why.
                        // Thus, this stupid hack to achieve byte-perfection.
                        //var animNameCheck = AnimFileName.ToLower();
                        //if (!(animNameCheck.EndsWith(".hkt") 
                        //    || (format == TAEFormat.SDT && animNameCheck.EndsWith("hkt")) 
                        //    || animNameCheck.EndsWith(".hkx") 
                        //    || animNameCheck.EndsWith(".sib") 
                        //    || animNameCheck.EndsWith(".hkxwin")))
                        //    AnimFileName = "";
                    }






                    return header;
                }

                /// <summary>
                /// Whether this header is completely null and unused
                /// </summary>
                public bool IsNullHeader { get; set; } = false;

                public string AnimFileName { get; set; }

                /// <summary>
                /// Gets a clone of this not tied by reference.
                /// </summary>
                public abstract AnimFileHeader GetClone();

                /// <summary>
                /// Standard AnimFileHeader with three flags, one of which can import motion data from another animation.
                /// </summary>
                public sealed class Standard : AnimFileHeader
                {
                    public Standard()
                    {
                        Type = AnimFileHeaderType.Standard;
                    }

                    /// <summary>
                    /// Gets a clone of this not tied by reference.
                    /// </summary>
                    public override AnimFileHeader GetClone()
                    {
                        var newClone = new Standard();
                        newClone.IsNullHeader = IsNullHeader;
                        newClone.AnimFileName = AnimFileName;
                        newClone.Type = Type;
                        newClone.IsLoopByDefault = IsLoopByDefault;
                        newClone.AllowDelayLoad = AllowDelayLoad;
                        newClone.ImportsHKX = ImportsHKX;

                        newClone.ImportHKXSourceAnimID = ImportHKXSourceAnimID;

                        return newClone;
                    }

                    /// <summary>
                    /// Makes the animation loop by default. Only relevant for animations not controlled by
                    /// ESD or HKS such as ObjAct animations.
                    /// </summary>
                    public bool IsLoopByDefault { get; set; } = false;

                    /// <summary>
                    /// Whether to import the HKX (actual motion data) of the animation with the ID of <see cref="ImportHKXSourceAnimID"/>.
                    /// </summary>
                    public bool ImportsHKX { get; set; } = false;

                    /// <summary>
                    /// Whether to allow this animation to be loaded from delayload anibnds such as the c0000_cXXXX.anibnd player throw anibnds.
                    /// </summary>
                    public bool AllowDelayLoad { get; set; } = false;

                    /// <summary>
                    /// Anim ID to import HKX from. Only functional if
                    /// <see cref="ImportsHKX"/> is enabled.
                    /// </summary>
                    public int ImportHKXSourceAnimID { get; set; } = 0;

                    internal override void ReadInner(BinaryReaderEx br, TAEFormat format)
                    {
                        if (format == TAEFormat.DESR)
                        {
                            ImportHKXSourceAnimID = br.ReadInt32();

                            IsLoopByDefault = br.ReadByte() != 0;
                            ImportsHKX = br.ReadByte() != 0;
                            AllowDelayLoad = br.ReadByte() != 0;
                            AllowDelayLoad = false;
                            br.ReadByte();

                            
                        }
                        else
                        {
                            IsLoopByDefault = br.ReadByte() != 0;
                            ImportsHKX = br.ReadByte() != 0;
                            AllowDelayLoad = br.ReadByte() != 0;

                            if (format == TAEFormat.DES)
                                AllowDelayLoad = false;

                            br.ReadByte();

                            ImportHKXSourceAnimID = br.ReadInt32();
                        }
                        
                    }

                    internal override void WriteInner(BinaryWriterEx bw, TAEFormat format)
                    {
                        if (format is TAEFormat.DESR)
                        {
                            bw.WriteInt32(ImportHKXSourceAnimID);
                            bw.WriteBoolean(IsLoopByDefault);
                            bw.WriteBoolean(ImportsHKX);
                            bw.WriteBoolean(false);
                            bw.WriteByte(0);
                        }
                        else
                        {
                            bw.WriteBoolean(IsLoopByDefault);
                            bw.WriteBoolean(ImportsHKX);
                            bw.WriteBoolean(format != TAEFormat.DES && AllowDelayLoad);
                            bw.WriteByte(0);

                            bw.WriteInt32(ImportHKXSourceAnimID);
                        }
                        
                    }
                }

                /// <summary>
                /// AnimFileHeader that signifies that the animation fully imports the motion data and all actions from another animation.
                /// </summary>
                public sealed class ImportOtherAnim : AnimFileHeader
                {
                    public ImportOtherAnim()
                    {
                        Type = AnimFileHeaderType.ImportOtherAnim;
                    }

                    /// <summary>
                    /// Gets a clone of this not tied by reference.
                    /// </summary>
                    public override AnimFileHeader GetClone()
                    {
                        var newClone = new ImportOtherAnim();
                        newClone.IsNullHeader = IsNullHeader;
                        newClone.AnimFileName = AnimFileName;
                        newClone.Type = Type;
                        newClone.ImportFromAnimID = ImportFromAnimID;
                        newClone.Unknown = Unknown;

                        return newClone;
                    }

                    /// <summary>
                    /// ID of animation from which to import motion dat and all actions.
                    /// </summary>
                    public int ImportFromAnimID { get; set; } = 0;

                    /// <summary>
                    /// Unknown usage.
                    /// </summary>
                    public int Unknown { get; set; } = -1;

                    internal override void ReadInner(BinaryReaderEx br, TAEFormat format)
                    {
                        if (format is TAEFormat.DESR)
                        {
                            Unknown = br.ReadInt32();
                            ImportFromAnimID = br.ReadInt32();
                        }
                        else
                        {
                            ImportFromAnimID = br.ReadInt32();
                            Unknown = br.ReadInt32();
                        }
                        

                        if (format == TAEFormat.DES)
                            br.Pad(0x10);
                    }

                    internal override void WriteInner(BinaryWriterEx bw, TAEFormat format)
                    {
                        if (format == TAEFormat.DESR)
                        {
                            bw.WriteInt32(Unknown);
                            bw.WriteInt32(ImportFromAnimID);
                        }
                        else
                        {
                            bw.WriteInt32(ImportFromAnimID);
                            bw.WriteInt32(Unknown);
                        }
                        

                        if (format == TAEFormat.DES)
                            bw.Pad(0x10);
                    }
                }
            }

            /// <summary>
            /// ID number of this animation.
            /// </summary>
            public long ID { get; set; }

            /// <summary>
            /// Actions in this animation.
            /// </summary>
            public List<Action> Actions;

            /// <summary>
            /// Track containing actions. Unused in character files of DES/DS1 but used basically everywhere else.
            /// </summary>
            public List<ActionTrack> ActionTracks;

            /// <summary>
            /// The animation file header of this animation entry.
            /// </summary>
            public AnimFileHeader Header { get; set; } = null;

            /// <summary>
            /// Creates a new empty animation with the specified properties.
            /// </summary>
            public Animation(long id, AnimFileHeader miniHeader)
            {
                ID = id;
                Header = miniHeader;
                Actions = new List<Action>();
                ActionTracks = new List<ActionTrack>();
            }

            internal Animation(BinaryReaderEx br, TAEFormat format,
                out bool lastActionNeedsParamGen, out long animFileOffset,
                out long lastActionParamOffset)
            {
                lastActionNeedsParamGen = false;
                lastActionParamOffset = 0;
                ID = br.ReadVarint();
                long offset = br.ReadVarint();

                if (format == TAEFormat.DES) // Not in DESR
                {
                    br.Pad(0x10);
                }

                br.StepIn(offset);
                {
                    int actionCount;
                    long actionHeadersOffset;
                    int actionTrackCount;
                    long actionTracksOffset;
                    long timesOffset;

                    if (format == TAEFormat.DS1 || format == TAEFormat.DES)
                    {
                        actionCount = br.ReadInt32();
                        actionHeadersOffset = br.ReadVarint();
                        actionTrackCount = br.ReadInt32();
                        actionTracksOffset = br.ReadVarint();
                        br.ReadInt32(); // Times count
                        timesOffset = br.ReadVarint(); // Times offset
                        animFileOffset = br.ReadVarint();

                        //For DeS assert 5 int32 == 0 here
                        if (format == TAEFormat.DES)
                        {
                            for (int i = 0; i < 5; i++)
                                br.AssertInt32(0);
                        }
                    }
                    else if (format == TAEFormat.DESR)
                    {
                        actionCount = br.ReadInt32();
                        actionTrackCount = br.ReadInt32();
                        br.ReadInt32(); // Times count
                        br.AssertInt32(0);
                        actionHeadersOffset = br.ReadVarint();
                        actionTracksOffset = br.ReadVarint();
                        timesOffset = br.ReadVarint(); // Times offset
                        animFileOffset = br.ReadVarint();
                    }
                    else
                    {
                        actionHeadersOffset = br.ReadVarint();
                        actionTracksOffset = br.ReadVarint();
                        timesOffset = br.ReadVarint(); // Times offset
                        animFileOffset = br.ReadVarint();
                        actionCount = br.ReadInt32();
                        actionTrackCount = br.ReadInt32();
                        br.ReadInt32(); // Times count
                        br.AssertInt32(0);
                    }

                    var actionHeaderOffsets = new List<long>(actionCount);
                    var actionParameterOffsets = new List<long>(actionCount);
                    Actions = new List<Action>(actionCount);
                    br.StepIn(actionHeadersOffset);
                    {
                        for (int i = 0; i < actionCount; i++)
                        {
                            actionHeaderOffsets.Add(br.Position);
                            Actions.Add(Action.Read(br, out long pOffset, format));
                            actionParameterOffsets.Add(pOffset);

                            if (i > 0)
                            {
                                //  Go to previous action's parameters
                                br.StepIn(actionParameterOffsets[i - 1]);
                                {
                                    // Read the space between the previous action's parameter start and the start of this action data.
                                    long gapBetweenActionParamOffsets = actionParameterOffsets[i] - actionParameterOffsets[i - 1];
                                    // Subtract to account for the current action's type and offset 
                                    Actions[i - 1].ReadParameters(br, (int)(gapBetweenActionParamOffsets - (br.VarintLong ? 16 : 8)));
                                }
                                br.StepOut();
                            }
                        }
                    }
                    br.StepOut();

                    if (actionCount > 0)
                    {
                        if (actionTracksOffset == 0)
                        {
                            lastActionNeedsParamGen = true;
                            lastActionParamOffset = actionParameterOffsets[actionCount - 1];
                        }
                        else
                        {
                            // Go to last actions's parameters
                            br.StepIn(actionParameterOffsets[actionCount - 1]);
                            {
                                // Read the space between the last action's parameter start and the start of the action tracks.
                                Actions[actionCount - 1].ReadParameters(br, (int)(actionTracksOffset - actionParameterOffsets[actionCount - 1]));
                            }
                            br.StepOut();
                        }
                    }

                    ActionTracks = new List<ActionTrack>(actionTrackCount);
                    br.StepIn(actionTracksOffset);
                    {
                        for (int i = 0; i < actionTrackCount; i++)
                            ActionTracks.Add(new ActionTrack(br, actionHeaderOffsets, format));
                    }
                    br.StepOut();

                    for (int gi = 0; gi < ActionTracks.Count; gi++)
                    {
                        foreach (var idx in ActionTracks[gi].indices)
                        {
                            var act = Actions[idx];
                            if (act.TrackIndex < 0)
                                act.TrackIndex = gi;
                            else
                                throw new Exception("TAE Action in multiple tracks...");
                        }
                    }

                    br.StepIn(animFileOffset);
                    {
                        Header = AnimFileHeader.Read(br, format, timesOffset);
                    }
                    br.StepOut();
                }
                br.StepOut();
            }

            internal void WriteHeader(BinaryWriterEx bw, int i, TAEFormat format)
            {
                bw.WriteVarint(ID);
                bw.ReserveVarint($"AnimationOffset{i}");
                if (format == TAEFormat.DES)
                    bw.Pad(0x10);
            }

            internal void WriteBody(BinaryWriterEx bw, int i, TAEFormat format)
            {
                bw.FillVarint($"AnimationOffset{i}", bw.Position);

                //ActionTracks.Clear();
                //foreach (var act in Actions)
                //{
                //    if (act.Track != null && !ActionTracks.Contains(act.Track))
                //        ActionTracks.Add(act.Track);
                //}

                if (format is TAEFormat.DS1 or TAEFormat.DES)
                {
                    bw.WriteInt32(Actions.Count);
                    bw.ReserveVarint($"ActionHeadersOffset{i}");
                    bw.WriteInt32(ActionTracks.Count);
                    bw.ReserveVarint($"ActionTrackHeadersOffset{i}");
                    bw.ReserveInt32($"TimesCount{i}");
                    bw.ReserveVarint($"TimesOffset{i}");
                    bw.ReserveVarint($"AnimFileOffset{i}");
                    //For DeS write 5 int32 == 0
                    if (format == TAEFormat.DES)
                        for (int j = 0; j < 5; j++)
                            bw.WriteInt32(0);
                }
                else if (format is TAEFormat.DESR)
                {
                    bw.WriteInt32(Actions.Count);
                    bw.WriteInt32(ActionTracks.Count);
                    bw.ReserveInt32($"TimesCount{i}");
                    bw.WriteInt32(0);
                    bw.ReserveVarint($"ActionHeadersOffset{i}");
                    bw.ReserveVarint($"ActionTrackHeadersOffset{i}");
                    bw.ReserveVarint($"TimesOffset{i}");
                    bw.ReserveVarint($"AnimFileOffset{i}");
                }
                else
                {
                    bw.ReserveVarint($"ActionHeadersOffset{i}");
                    bw.ReserveVarint($"ActionTrackHeadersOffset{i}");
                    bw.ReserveVarint($"TimesOffset{i}");
                    bw.ReserveVarint($"AnimFileOffset{i}");
                    bw.WriteInt32(Actions.Count);
                    bw.WriteInt32(ActionTracks.Count);
                    bw.ReserveInt32($"TimesCount{i}");
                    bw.WriteInt32(0);
                }
            }

            internal void WriteAnimFile(BinaryWriterEx bw, int i, TAEFormat format)
            {
                bw.FillVarint($"AnimFileOffset{i}", bw.Position);
                Header.Write(bw, format);
            }

            internal Dictionary<float, long> WriteTimes(BinaryWriterEx bw, int animIndex, TAEFormat format)
            {
                var times = new SortedSet<float>();

                foreach (Action evt in Actions)
                {
                    times.Add(evt.StartTime);
                    times.Add(evt.MemeEndTime);
                }

                bw.FillInt32($"TimesCount{animIndex}", times.Count);

                if (times.Count == 0)
                    bw.FillVarint($"TimesOffset{animIndex}", 0);
                else
                    bw.FillVarint($"TimesOffset{animIndex}", bw.Position);

                var timeOffsets = new Dictionary<float, long>();
                foreach (float time in times)
                {
                    timeOffsets[time] = bw.Position;
                    bw.WriteSingle(time);
                }

                if (format is not TAEFormat.DS1)
                    bw.Pad(0x10);

                return timeOffsets;
            }

            internal List<long> WriteActionHeaders(BinaryWriterEx bw, int animIndex, Dictionary<float, long> timeOffsets, TAEFormat format)
            {
                var actionHeaderOffsets = new List<long>(Actions.Count);
                if (Actions.Count > 0)
                {
                    bw.FillVarint($"ActionHeadersOffset{animIndex}", bw.Position);
                    for (int i = 0; i < Actions.Count; i++)
                    {
                        actionHeaderOffsets.Add(bw.Position);
                        Actions[i].WriteHeader(bw, animIndex, i, timeOffsets, format);
                    }
                }
                else
                {
                    bw.FillVarint($"ActionHeadersOffset{animIndex}", 0);
                }
                return actionHeaderOffsets;
            }

            internal void WriteActionData(BinaryWriterEx bw, int i, TAEFormat format)
            {
                for (int j = 0; j < Actions.Count; j++)
                    Actions[j].WriteData(bw, i, j, format);
            }

            internal void WriteActionTrackHeaders(BinaryWriterEx bw, int i, TAEFormat format, bool saveWithActionTracksStripped)
            {
                if (ActionTracks.Count > 0 && !saveWithActionTracksStripped)
                {
                    bw.FillVarint($"ActionTrackHeadersOffset{i}", bw.Position);
                    for (int j = 0; j < ActionTracks.Count; j++)
                    {
                        ActionTracks[j].indices = Actions.Where(ev => ev.TrackIndex == j).Select(ev => Actions.IndexOf(ev)).ToList();
                        ActionTracks[j].WriteHeader(bw, i, j, format);
                    }
                }
                else
                {
                    bw.FillVarint($"ActionTrackHeadersOffset{i}", 0);
                }
            }

            internal void WriteActionTrackData(BinaryWriterEx bw, int i, List<long> actionHeaderOffsets, TAEFormat format)
            {
                for (int j = 0; j < ActionTracks.Count; j++)
                    ActionTracks[j].WriteData(bw, i, j, actionHeaderOffsets, format);
            }
        }

    }
}
