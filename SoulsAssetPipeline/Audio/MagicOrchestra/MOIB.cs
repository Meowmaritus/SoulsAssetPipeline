using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SoulsFormats;

namespace SoulsAssetPipeline.Audio.MagicOrchestra
{
    public class MOIB : SoulsFile<MOIB>
    {
        public bool BigEndian { get; set; }
        public MOIBHeader Header { get; set; }
        public List<Entry> Entries { get; set; }
        
        protected override void Read(BinaryReaderEx br)
        {
            BigEndian = true;
            br.BigEndian = BigEndian;

            Header = new MOIBHeader(br);

            Entries = new List<Entry>(Header.entryCount);
            for (int i = 0; i < Header.entryCount; i++)
                Entries.Add(new Entry(br, Header));
        }

        public class MOIBHeader
        {
            public string magic { get; set; }
            public short unk04 { get; set; }
            public short unk06 { get; set; }
            public int entryCount { get; set; }
            public int unk0C { get; set; }
            internal MOIBHeader(BinaryReaderEx br)
            {
                magic = br.AssertASCII("MOSI");
                unk04 = br.AssertInt16(2);
                unk06 = br.AssertInt16(1, 2, 3);
                entryCount = br.ReadInt32();
                unk0C = br.AssertInt32(0);
            }
        }

        public class Entry
        {
            public int id { get; set; } // Or just index from what I've seen
            public byte unk04 { get; set; }
            public byte unk05 { get; set; }
            public byte unk06 { get; set; }
            public byte unk07 { get; set; }
            public byte unk08 { get; set; } // Count of something? Matches number of unk09-unk0B which are not FF
            public byte unk09 { get; set; }
            public byte unk0A { get; set; }
            public byte unk0B { get; set; }
            public float unk0C { get; set; }
            public float unk10 { get; set; }
            public byte unk14 { get; set; }
            public byte unk15 { get; set; }
            public short unk16 { get; set; }
            public int unk18 { get; set; }
            internal Entry(BinaryReaderEx br, MOIBHeader header)
            {
                id = br.ReadInt32(); // Or just index from what I've seen
                unk04 = br.ReadByte();
                unk05 = br.ReadByte();
                unk06 = br.ReadByte();
                unk07 = br.ReadByte();
                unk08 = br.ReadByte(); // Count of something? Matches number of unk09-unk0B which are not FF
                unk09 = br.ReadByte();
                unk0A = br.ReadByte();
                unk0B = br.ReadByte();
                unk0C = br.ReadSingle();
                unk10 = br.ReadSingle();
                if(header.unk06 > 1)
                {
                                    unk14 = br.ReadByte();
                unk15 = br.ReadByte();
                unk16 = br.AssertInt16(0);
                unk18 = br.AssertInt32(0);
                }
            }
        }
    }
}
