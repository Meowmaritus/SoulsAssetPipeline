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


    
        public int GetTotalFrameCount()
        {
            int count = 0;
            foreach (var c in Cuts)
                count += (int)c.NumFrames;
            return count;
        }

        public List<CameraFrame> GetAllCameraFrames()
        {
            List<CameraFrame> result = new List<CameraFrame>();
            foreach (var c in Cuts)
                result.AddRange(c.CameraAnimation);
            return result;
        }

        public List<FoVData> GetAllFoVData()
        {
            List<FoVData> result = new List<FoVData>();
            foreach (var c in Cuts)
                result.AddRange(c.FoVDataList);
            return result;
        }


        public List<CamCut> Cuts = new List<CamCut>();

        public class CamCut
        {
            public string CameraName { get; set; }

            public uint NumFrames { get; set; }

            public uint NumFoVData { get; set; }
            public uint UnkCount { get; set; }

            public float InitialFoV { get; set; }

            public List<FoVData> FoVDataList;

            public List<CameraFrame> CameraAnimation;
            public FrameRef[] FrameRefs;
        }

        protected override void Read(BinaryReaderEx br)
        {
            br.BigEndian = false;
            br.VarintLong = false;

            bool isBigEndian = br.AssertInt32(1, 0x1000000) != 1;

            br.BigEndian = isBigEndian;


            //br.Skip(0x24);
            br.Position = 0x20;
            uint animationDataStartOffset = br.ReadUInt32();

            br.Position = 0x28;
            uint NumAnimValues = br.ReadUInt32();
            Vector3[] AnimationData = new Vector3[NumAnimValues];

            //br.Skip(0x14C);
            br.Position = 0x84;
            uint firstCutStartOffset = br.ReadUInt32();

            Cuts = new List<CamCut>();

            br.Position = firstCutStartOffset;

            while (br.Position < br.Length)
            {
                var c = new CamCut();

               
                c.CameraName = br.ReadASCII();

                br.Pad(4);
                //br.Skip(4);
                uint framesStartOffset = br.ReadUInt32();

                c.NumFrames = br.ReadUInt32();
                c.FrameRefs = new FrameRef[c.NumFrames];
                c.CameraAnimation = new List<CameraFrame>((int)c.NumFrames);

                //br.Skip(0x20);

                br.Position = framesStartOffset;

                for (int i = 0; i < c.NumFrames; i++)
                {
                    c.FrameRefs[i].Index = br.ReadUInt32();
                    c.FrameRefs[i].PositionIndex = br.ReadUInt32();
                    c.FrameRefs[i].PositionDiffPrevIndex1 = br.ReadUInt32();
                    c.FrameRefs[i].PositionDiffPrevIndex2 = br.ReadUInt32();
                    c.FrameRefs[i].RotationIndex = br.ReadUInt32();
                    c.FrameRefs[i].RotationDiffPrevIndex1 = br.ReadUInt32();
                    c.FrameRefs[i].RotationDiffPrevIndex2 = br.ReadUInt32();
                    c.FrameRefs[i].ScaleIndex = br.ReadUInt32();
                }

                c.InitialFoV = br.ReadSingle();

                var nextCameraOffset = br.ReadInt32();


                


                c.NumFoVData = br.ReadUInt32();
                c.UnkCount = br.ReadUInt32();

                Cuts.Add(c);

                if (nextCameraOffset == 0 || br.GetByte(nextCameraOffset) == 0)
                    break;



                br.Position = nextCameraOffset;

            }

            br.Position = animationDataStartOffset;

            

            foreach (var c in Cuts)
            {
                c.FoVDataList = new List<FoVData>((int)c.NumFoVData);

                for (int i = 0; i < c.NumFoVData; i++)
                {
                    c.FoVDataList.Add(new FoVData() { FrameIdx = br.ReadUInt32(), FoV = br.ReadSingle(), TanIn = br.ReadSingle(), TanOut = br.ReadSingle() });
                }
            }






            
            for (int i = 0; i < NumAnimValues; i++)
            {
                AnimationData[i].X = br.ReadSingle();
                AnimationData[i].Y = br.ReadSingle();
                AnimationData[i].Z = br.ReadSingle();
            }


            //Done reading

            FrameRef currFrameRef;

            foreach (var c in Cuts)
            {
                for (int i = 0; i < c.NumFrames; i++)
                {
                    CameraFrame CamFrame = new CameraFrame();
                    currFrameRef = c.FrameRefs[i];
                    CamFrame.Index = currFrameRef.Index;
                    CamFrame.Position = AnimationData[currFrameRef.PositionIndex];
                    CamFrame.PositionDiffPrev = AnimationData[currFrameRef.RotationDiffPrevIndex1];
                    CamFrame.Rotation = AnimationData[currFrameRef.RotationIndex];
                    CamFrame.RotationDiffPrev = AnimationData[currFrameRef.RotationDiffPrevIndex1];
                    CamFrame.Scale = AnimationData[currFrameRef.ScaleIndex];
                    c.CameraAnimation.Add(CamFrame);
                }
            }

        }

        public struct FrameRef
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
