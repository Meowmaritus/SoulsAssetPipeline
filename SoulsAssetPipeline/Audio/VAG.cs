using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SoulsFormats;
using static SoulsAssetPipeline.Audio.ATRAC3P;
using static SoulsAssetPipeline.Audio.VAG;

namespace SoulsAssetPipeline.Audio
{
    public class VAG : SoulsFile<VAG>
    {
        public bool BigEndian { get; set; }
        public VAGHeader Header { get; set; }
        public List<Block> Blocks { get; set; }
        protected override void Read(BinaryReaderEx br)
        {
            BigEndian = true;
            br.BigEndian = BigEndian;

            Header = new VAGHeader(br);

            Blocks = new List<Block>();
            for (int i = 0; i < Header.WaveformDataSize / 16; i++)
                Blocks.Add(new Block(br));
        }
        public class VAGHeader
        {
            public string Magic { get; set; }
            public uint Version { get; set; }
            public uint Reserved { get; set; }
            public uint WaveformDataSize { get; set; }
            public uint SampleRate { get; set; }
            public ushort BaseVolForLeftChannel { get; set; }
            public ushort BaseVolForRightChannel { get; set; }
            public ushort BasePitch { get; set; }
            public ushort BaseADSR1 { get; set; }
            public ushort BaseADSR2 { get; set; }
            public ushort Reserved2 { get; set; }
            public string TrackName { get; set; }
            internal VAGHeader(BinaryReaderEx br)
            {
                Magic = br.AssertASCII("VAGp");
                Version = br.AssertUInt32(2);
                Reserved = br.AssertUInt32(0);
                WaveformDataSize = br.ReadUInt32();
                SampleRate = br.ReadUInt32();
                BaseVolForLeftChannel = br.ReadUInt16();
                BaseVolForRightChannel = br.ReadUInt16();
                BasePitch = br.ReadUInt16();
                BaseADSR1 = br.ReadUInt16();
                BaseADSR2 = br.ReadUInt16();
                Reserved2 = br.ReadUInt16();
                TrackName = br.ReadFixStr(16);
            }
        }

        public class Block
        {
            public byte unk04 { get; set; }
            public byte loopType { get; set; }
            public byte[] data { get; set; }
            internal Block(BinaryReaderEx br)
            {
                unk04 = br.ReadByte();
                loopType = br.AssertByte(0, 1, 2, 3, 6, 7);
                data = br.ReadBytes(14);
            }
        }
    }
}
