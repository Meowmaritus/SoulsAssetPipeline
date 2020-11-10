using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;

using SoulsFormats;
using System.Runtime.InteropServices;

namespace SoulsAssetPipeline.Animation.SIBCAM
{
    public class SIBCAM : SoulsFile<SIBCAM>
    {
        /// <summary>
        /// Whether the format is big endian.
        /// Only valid for DS1 files.
        /// </summary>
        public bool BigEndian { get; set; }

        public string CameraName { get; set; }

        public uint NumFrames { get; set; }

        public uint NumFoVData { get; set; }

        public float InitialFoV { get; set; }

        public List<CameraFrame> CameraAnimation;

        public List<FoVData> FoVDataList;

        protected override void Read(BinaryReaderEx br)
        {
            br.BigEndian = false;
            br.VarintLong = false;

            bool isBigEndian = br.AssertInt32(1, 0x1000000) != 1;

            br.BigEndian = isBigEndian;

            br.Skip(0x24);

            uint NumAnimValues = br.ReadUInt32();
            Vector3[] AnimationData = new Vector3[NumAnimValues];

            br.Skip(0x14C);

            CameraName = br.ReadASCII();

            br.Pad(4);
            br.Skip(4);

            NumFrames = br.ReadUInt32();
            FrameRef[] FrameRefs = new FrameRef[NumFrames];
            CameraAnimation = new List<CameraFrame>((int)NumFrames);

            br.Skip(0x20);

            for (int i = 0; i < NumFrames; i++)
            {
                FrameRefs[i].Index = br.ReadUInt32();
                FrameRefs[i].PositionIndex = br.ReadUInt32();
                FrameRefs[i].PositionDiffPrevIndex1 = br.ReadUInt32();
                FrameRefs[i].PositionDiffPrevIndex2 = br.ReadUInt32();
                FrameRefs[i].RotationIndex = br.ReadUInt32();
                FrameRefs[i].RotationDiffPrevIndex1 = br.ReadUInt32();
                FrameRefs[i].RotationDiffPrevIndex2 = br.ReadUInt32();
                FrameRefs[i].ScaleIndex = br.ReadUInt32();
            }

            InitialFoV = br.ReadSingle();

            br.ReadInt32();
            NumFoVData = br.ReadUInt32();
            br.ReadInt32();

            FoVDataList = new List<FoVData>((int)NumFoVData);

            for (int i = 0; i < NumFoVData; i++)
            {
                FoVDataList[i].FrameIdx = br.ReadUInt32();
                FoVDataList[i].FoV = br.ReadSingle();
                FoVDataList[i].TanIn = br.ReadSingle();
                FoVDataList[i].TanOut = br.ReadSingle();
            }

            for (int i = 0; i < NumAnimValues; i++)
            {
                AnimationData[i].X = br.ReadSingle();
                AnimationData[i].Y = br.ReadSingle();
                AnimationData[i].Z = br.ReadSingle();
            }

            //Done reading

            FrameRef currFrameRef;

            for (int i = 0; i < NumFrames; i++)
            {
                currFrameRef = FrameRefs[i];
                CameraAnimation[i].Index = currFrameRef.Index;
                CameraAnimation[i].Position = AnimationData[currFrameRef.PositionIndex];
                CameraAnimation[i].PositionDiffPrev = AnimationData[currFrameRef.RotationDiffPrevIndex1];
                CameraAnimation[i].Rotation = AnimationData[currFrameRef.RotationIndex];
                CameraAnimation[i].RotationDiffPrev = AnimationData[currFrameRef.RotationDiffPrevIndex1];
                CameraAnimation[i].Scale = AnimationData[currFrameRef.ScaleIndex];
            }

        }

        internal struct FrameRef
        {
            public uint Index;
            public uint PositionIndex;
            public uint PositionDiffPrevIndex1;
            public uint PositionDiffPrevIndex2;
            public uint RotationIndex;
            public uint RotationDiffPrevIndex1;
            public uint RotationDiffPrevIndex2;
            public uint ScaleIndex;
        }

        public class CameraFrame
        {
            public uint Index;
            public Vector3 Position;
            public Vector3 PositionDiffPrev;
            public Vector3 Rotation;
            public Vector3 RotationDiffPrev;
            public Vector3 Scale;
        }

        public class FoVData
        {
            public uint FrameIdx;
            public float FoV;
            public float TanIn;
            public float TanOut;
        }
    }
}
