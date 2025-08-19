
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SoulsFormats;
using static SoulsAssetPipeline.Audio.ATRAC3P;
using static SoulsAssetPipeline.Audio.MagicOrchestra.MOIB;

namespace SoulsAssetPipeline.Audio
{
    public class ATRAC3P : SoulsFile<ATRAC3P>
    {
        public bool BigEndian { get; set; }
        public RiffHeader Header { get; set; }
        public Fmt fmt { get; set; }
        public Fact fact { get; set; }
        public Smpl smpl { get; set; }
        public byte[] data { get; set; }
        //public List<Chunk> Chunks { get; set; }
        protected override void Read(BinaryReaderEx br)
        {
            BigEndian = false;
            br.BigEndian = BigEndian;

            Header = new RiffHeader(br);

            //Chunks = new List<Chunk>();
            while (br.Position != br.Length)
            {
                var type = br.ReadASCII(4);
                var chunkSize = br.ReadInt32();
                switch (type)
                {
                    case "fmt ":
                        fmt = new Fmt(br); break;
                    case "fact":
                        fact = new Fact(br); break;
                    case "smpl":
                        smpl = new Smpl(br); break;
                    case "data":
                        data = br.ReadBytes(chunkSize); break;
                }
            }
        }

        public class RiffHeader
        {
            public string magic { get; set; }
            public int fileSize { get; set; }
            public string type { get; set; }
            internal RiffHeader(BinaryReaderEx br)
            {
                magic = br.AssertASCII("RIFF");
                fileSize = br.AssertInt32((int)br.Length - 8);
                type = br.AssertASCII("WAVE");
            }
        }

        public class Chunk
        {
            public string type { get; set; }
            public int chunkSize { get; set; }
            public Fmt fmt { get; set; }
            public Fact fact { get; set; }
            public Smpl smpl { get; set; }
            public byte[] data { get; set; }
            internal Chunk(BinaryReaderEx br)
            {
                type = br.ReadASCII(4);
                chunkSize = br.ReadInt32();
                switch (type)
                {
                    case "fmt ":
                        fmt = new Fmt(br); break;
                    case "fact":
                        fact = new Fact(br); break;
                    case "smpl":
                        smpl = new Smpl(br); break;
                    case "data":
                        data = br.ReadBytes(chunkSize); break;
                }
            }
        }

        public class Fmt
        {
            public short formatType { get; set; }
            public short channelCount { get; set; }
            public int sampleRate { get; set; }
            public int dataRate { get; set; }
            public int blockSize { get; set; }
            public short fmtDataSize { get; set; }
            public short samplesPerBlock { get; set; }
            public int channelMask { get; set; }
            public byte[] codecGuid;
            public short unk00 { get; set; }
            public short unk04 { get; set; }
            public int unk08 { get; set; }
            public int unk0C { get; set; }
            internal Fmt(BinaryReaderEx br)
            {
                formatType = br.ReadInt16();
                channelCount = br.ReadInt16();
                sampleRate = br.ReadInt32();
                dataRate = br.ReadInt32();
                blockSize = br.ReadInt32();
                fmtDataSize = br.ReadInt16();
                samplesPerBlock = br.ReadInt16();
                channelMask = br.ReadInt32();
                codecGuid = br.ReadBytes(16);
                unk00 = br.ReadInt16();
                unk04 = br.ReadInt16();
                unk08 = br.ReadInt32();
                unk0C = br.ReadInt32();
            }
        }

        public class Fact
        {
            public int decAudioLengthInSamples { get; set; }
            public int unk04 { get; set; }
            public int encoderDelay { get; set; }
            internal Fact(BinaryReaderEx br)
            {
                decAudioLengthInSamples = br.ReadInt32();
                unk04 = br.ReadInt32();
                encoderDelay = br.ReadInt32();
            }
        }

        public class Smpl
        {
            public int Manufacturer { get; set; }
            public int Product { get; set; }
            public int SamplePeriod { get; set; }
            public int MIDIUnityNote { get; set; }
            public int MIDIPitchFraction { get; set; }
            public int SMPTEFormat { get; set; }
            public int SMPTEOffset { get; set; }
            public int NumSampleLoops { get; set; }
            public int SamplerData { get; set; }
            public int CuePointID { get; set; }
            public int Type { get; set; }
            public int loopStart { get; set; }
            public int loopEnd { get; set; }
            public int Fraction { get; set; }
            public int PlayCount { get; set; }
            internal Smpl(BinaryReaderEx br)
            {
                Manufacturer = br.ReadInt32();
                Product = br.ReadInt32();
                SamplePeriod = br.ReadInt32();
                MIDIUnityNote = br.ReadInt32();
                MIDIPitchFraction = br.ReadInt32();
                SMPTEFormat = br.ReadInt32();
                SMPTEOffset = br.ReadInt32();
                NumSampleLoops = br.AssertInt32(1);
                SamplerData = br.ReadInt32();
                CuePointID = br.ReadInt32();
                Type = br.ReadInt32();
                loopStart = br.ReadInt32();
                loopEnd = br.ReadInt32();
                Fraction = br.ReadInt32();
                PlayCount = br.ReadInt32();
            }

            public Smpl()
            {
                Manufacturer = 0;
                Product = 0;
                SamplePeriod = 0;
                MIDIUnityNote = 0;
                MIDIPitchFraction = 0;
                SMPTEFormat = 0;
                SMPTEOffset = 0;
                NumSampleLoops = 1;
                SamplerData = 0;
                CuePointID = 0;
                Type = 0;
                loopStart = 0;
                loopEnd = 0;
                Fraction = 0;
                PlayCount = 0;
            }
        }
    }
}
