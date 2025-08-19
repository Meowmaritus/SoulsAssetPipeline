using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SoulsAssetPipeline.Animation.SIBCAM
{
    public class SIBCAM2 : SoulsFile<SIBCAM2>
    {
        public int LastFrame;

        public List<Vector3> FrameData;

        public List<Cam> Cams = new List<Cam>();

        public class CamAnimation
        {
            public class FrameInfo
            {
                public int Index;
                public int PositionIndex;
                public int PositionDiffPrevIndex1;
                public int PositionDiffPrevIndex2;
                public int RotationIndex;
                public int RotationDiffPrevIndex1;
                public int RotationDiffPrevIndex2;
                public int ScaleIndex;
                public BakedData Baked;

                public struct BakedData
                {
                    public Vector3 Position;
                    public Vector3 PositionDiffPrev1;
                    public Vector3 PositionDiffPrev2;
                    public Vector3 Rotation;
                    public Vector3 RotationDiffPrev1;
                    public Vector3 RotationDiffPrev2;
                    public Vector3 Scale;
                }

                public void Read(BinaryReaderEx br)
                {
                    Index = br.ReadInt32();
                    PositionIndex = br.ReadInt32();
                    PositionDiffPrevIndex1 = br.ReadInt32();
                    PositionDiffPrevIndex2 = br.ReadInt32();
                    RotationIndex = br.ReadInt32();
                    RotationDiffPrevIndex1 = br.ReadInt32();
                    RotationDiffPrevIndex2 = br.ReadInt32();
                    ScaleIndex = br.ReadInt32();
                }
            }

            public List<FrameInfo> Frames = new List<FrameInfo>();
            public Vector3 Rotation1;
            public Vector3 Rotation2;

            public void Read(BinaryReaderEx br)
            {
                uint framesOffset = br.ReadUInt32();
                int framesCount = br.ReadInt32();
                br.AssertInt32(3);
                Rotation1 = br.ReadVector3();
                Rotation2 = br.ReadVector3();
                br.AssertInt32(0);

                Frames = new List<FrameInfo>(framesCount);
                br.StepIn(framesOffset);
                for (int i = 0; i < framesCount; i++)
                {
                    var f = new FrameInfo();
                    f.Read(br);
                    Frames.Add(f);
                }
                br.StepOut();
            }
        }

        public class UnkStruct
        {
            public class SubStruct1
            {
                public class SubFloatStruct
                {
                    public int Unk00;
                    public float Unk04;
                    public float Unk08;
                    public float Unk0C;
                    public float Unk10;
                    public float Unk14;
                    public float Unk18;
                    public float Unk1C;
                    public float Unk20;
                    public float Unk24;
                    public void Read(BinaryReaderEx br)
                    {
                        Unk00 = br.ReadInt32();
                        Unk04 = br.ReadSingle();
                        Unk08 = br.ReadSingle();
                        Unk0C = br.ReadSingle();
                        Unk10 = br.ReadSingle();
                        Unk14 = br.ReadSingle();
                        Unk18 = br.ReadSingle();
                        Unk1C = br.ReadSingle();
                        Unk20 = br.ReadSingle();
                        Unk24 = br.ReadSingle();
                    }
                }

                public int Unk00;
                public int Unk04;
                public float Unk08;
                public Vector3 Unk24;
                public List<SubFloatStruct> SubFloatStructs = new List<SubFloatStruct>();

                public void Read(BinaryReaderEx br)
                {
                    Unk00 = br.ReadInt32();
                    Unk04 = br.ReadInt32();
                    Unk08 = br.ReadSingle();
                    for (int i = 0; i < 6; i++)
                    {
                        br.AssertInt32(0);
                    }
                    Unk24 = br.ReadVector3();
                    uint unkFloatStructsOffset = br.ReadUInt32();
                    int unkFloatStructsCount = br.ReadInt32();

                    SubFloatStructs = new List<SubFloatStruct>(unkFloatStructsCount);
                    br.StepIn(unkFloatStructsOffset);
                    for (int i = 0; i < unkFloatStructsCount; i++)
                    {
                        var f = new SubFloatStruct();
                        f.Read(br);
                        SubFloatStructs.Add(f);
                    }
                    br.StepOut();
                }
            }



            public class SubStruct2
            {
                public uint PointerToSubStruct3_Todo;
                public int Unk34;
                public void Read(BinaryReaderEx br)
                {
                    for (int i = 0; i < 12; i++)
                    {
                        br.AssertInt32(0);
                    }
                    PointerToSubStruct3_Todo = br.ReadUInt32();
                    Unk34 = br.ReadInt32();
                }
            }

            public class SubStruct3
            {
                public uint PointerToCamera_Todo;
                public int Unk34;
                public void Read(BinaryReaderEx br)
                {
                    for (int i = 0; i < 12; i++)
                    {
                        br.AssertInt32(0);
                    }
                    PointerToCamera_Todo = br.ReadUInt32();
                    Unk34 = br.ReadInt32();
                }
            }

            public SubStruct1 Struct1;
            public SubStruct2 Struct2;
            public SubStruct3 Struct3;

            public void Read(BinaryReaderEx br)
            {
                uint struct1Offset = br.ReadUInt32();
                uint struct2Offset = br.ReadUInt32();
                uint struct3Offset = br.ReadUInt32();
                br.StepIn(struct1Offset);
                Struct1 = new SubStruct1();
                Struct1.Read(br);

                br.Position = struct2Offset;
                Struct2 = new SubStruct2();
                Struct2.Read(br);

                br.Position = struct3Offset;
                Struct3 = new SubStruct3();
                Struct3.Read(br);

                br.StepOut();
            }
        }

        public class FovStruct
        {
            public class FovSample
            {
                public int FrameIndex;
                public float Fov;
                public float TanIn;
                public float TanOut;
                public void Read(BinaryReaderEx br)
                {
                    FrameIndex = br.ReadInt32();
                    Fov = br.ReadSingle();
                    TanIn = br.ReadSingle();
                    TanOut = br.ReadSingle();
                }
            }
            public float DefaultFov;
            public List<FovSample> Samples = new List<FovSample>();

            public void Read(BinaryReaderEx br)
            {
                DefaultFov = br.ReadSingle();
                uint fovDataOffset = br.ReadUInt32();
                int fovDataCount = br.ReadInt32();
                br.AssertInt32(0);
                br.StepIn(fovDataOffset);
                Samples = new List<FovSample>(fovDataCount);
                for (int i = 0; i < fovDataCount; i++)
                {
                    var s = new FovSample();
                    s.Read(br);
                    Samples.Add(s);
                }
                br.StepOut();
            }
        }

        public class Cam
        {
            public string Name;
            public int Unk04;
            public short CamIndex;
            //public short Unk0A;
            public short NextCamIndex;
            public short Unk12;
            public Vector3 Translation;
            public Vector3 Rotation;
            public Vector3 Scale;
            public CamAnimation Animation;
            public UnkStruct UnknownStruct;
            public FovStruct Fov;

            public void Read(BinaryReaderEx br)
            {
                uint nameOffset = br.ReadUInt32();
                Unk04 = br.AssertInt32(1, 3, 4);
                CamIndex = br.ReadInt16();
                br.AssertInt16(-1);
                br.AssertInt32(-1);
                NextCamIndex = br.ReadInt16();
                Unk12 = br.AssertInt16(-1, 2);
                Translation = br.ReadVector3();
                Rotation = br.ReadVector3();
                Scale = br.ReadVector3();
                uint camAnimationOffset = br.ReadUInt32();
                br.AssertInt32(0);
                uint unkStructOffset = br.ReadUInt32();
                uint fovOffset = br.ReadUInt32();
                for (int i = 0; i < 43; i++)
                {
                    br.AssertInt32(0);
                }

                br.StepIn(nameOffset);
                Name = br.ReadASCII();

                br.Position = camAnimationOffset;
                Animation = new CamAnimation();
                Animation.Read(br);

                if (unkStructOffset != 0)
                {
                    br.Position = unkStructOffset;
                    UnknownStruct = new UnkStruct();
                    UnknownStruct.Read(br);
                }

                if (fovOffset != 0)
                {
                    br.Position = fovOffset;
                    Fov = new FovStruct();
                    Fov.Read(br);
                }

                br.StepOut();
            }
        }

        protected override void Read(BinaryReaderEx br)
        {
            br.BigEndian = false;
            int endianCheck = br.AssertInt32(1, 0x1000000);
            if (endianCheck == 0x1000000)
                br.BigEndian = true;

            br.AssertInt32(8);
            br.AssertInt32(0xC);
            br.AssertInt32(0x20051014);
            br.ReadInt32(); // No idea what this does, but its 0 on almost all files, 1 on just a few
            LastFrame = br.ReadInt32();
            int camsOffset = br.AssertInt32(0x84);
            int camsCount = br.ReadInt32();
            int frameDataOffset = br.ReadInt32();
            int frameDataLength = br.ReadInt32();
            int frameDataCount = br.ReadInt32();
            br.AssertInt32(0);
            int fileSize = br.ReadInt32();
            for (int i = 0; i < 20; i++)
            {
                br.AssertInt32(0);
            }

            Cams = new List<Cam>(camsCount);
            br.Position = camsOffset;
            for (int i = 0; i < camsCount; i++)
            {
                var newCam = new Cam();
                newCam.Read(br);
                Cams.Add(newCam);
            }

            FrameData = new List<Vector3>(frameDataCount);
            br.Position = frameDataOffset;
            for (int i = 0; i < frameDataCount; i++)
            {
                FrameData.Add(br.ReadVector3());
            }

            foreach (var c in Cams)
            {
                foreach (var f in c.Animation.Frames)
                {
                    f.Baked = new CamAnimation.FrameInfo.BakedData()
                    {
                        Position = FrameData[f.PositionIndex & 0x7FFFFFF],
                        PositionDiffPrev1 = FrameData[f.PositionDiffPrevIndex1 & 0x7FFFFFF],
                        PositionDiffPrev2 = FrameData[f.PositionDiffPrevIndex2 & 0x7FFFFFF],
                        Rotation = FrameData[f.RotationIndex & 0x7FFFFFF],
                        RotationDiffPrev1 = FrameData[f.RotationDiffPrevIndex1 & 0x7FFFFFF],
                        RotationDiffPrev2 = FrameData[f.RotationDiffPrevIndex2 & 0x7FFFFFF],
                        Scale = FrameData[f.ScaleIndex & 0x7FFFFFF],
                    };
                }
            }

        }

    }
}
