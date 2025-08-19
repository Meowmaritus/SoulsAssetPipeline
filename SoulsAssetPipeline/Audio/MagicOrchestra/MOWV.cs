using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoulsAssetPipeline.Audio.MagicOrchestra
{
    public class MOWV : SoulsFile<MOWV>
    {
        public MOWVHeader Header { get; set; }
        public List<Struct1> Struct1s { get; set; } = new List<Struct1>();
        public short[] VagIndices { get; set; }
        public List<Struct2> Struct2s { get; set; } = new List<Struct2>();
        protected override void Read(BinaryReaderEx br)
        {
            br.BigEndian = true;
            Header = new MOWVHeader(br);
            Struct1s = new List<Struct1>();
            VagIndices = new short[0];
            Struct2s = new List<Struct2>();
            if (Header.IsVag)
            {
                for (int i = 0; i < Header.count1; i++)
                    Struct1s.Add(new Struct1(br));
                VagIndices = br.ReadInt16s(Header.count1);
            }
            else
            {
                for (int i = 0; i < Header.count1 / 2; i++)
                    Struct2s.Add(new Struct2(br));
            }
        }

        public class MOWVHeader
        {
            public string magic { get; set; }
            public short unk04 { get; set; }
            public short unk08 { get; set; }
            public int fileSize { get; set; }
            public bool IsVag { get; set; }
            public int count1 { get; set; }
            public string name { get; set; }
            public byte unk0C { get; set; }
            public int unk10 { get; set; }
            public int unk14 { get; set; }
            public int unk18 { get; set; }
            public int unk1C { get; set; }
            public int unk24 { get; set; }
            internal MOWVHeader(BinaryReaderEx br)
            {
                magic = br.AssertASCII("MOWV");
                unk04 = br.AssertInt16(2);
                unk08 = br.AssertInt16(0);
                fileSize = br.ReadInt32();
                IsVag = fileSize != br.Length;
                count1 = br.ReadInt32();
                name = br.ReadASCII();
                br.Pad(0x10);
                unk14 = br.ReadInt32();
                unk18 = br.ReadInt32();
                unk1C = br.ReadInt32();
                unk24 = br.ReadInt32();
            }
        }
        public class Struct1
        {
            public int unk04 { get; set; }
            internal int vagIndexOffset { get; set; }
            public short vagIndex;
            internal Struct1(BinaryReaderEx br)
            {
                unk04 = br.ReadInt32();
                vagIndexOffset = br.ReadInt32();
                vagIndex = br.GetInt16(vagIndexOffset);
            }
        }
        public class Struct2
        {
            public byte loop { get; set; }
            public byte unk00 { get; set; }
            public byte unk04 { get; set; }
            public byte unk08 { get; set; }
            public int unk10 { get; set; }
            public int unk14 { get; set; }
            public int unk18 { get; set; }
            public int unk1C { get; set; }
            public byte[] uninitializedMemory { get; set; }
            internal Struct2(BinaryReaderEx br)
            {
                loop = br.ReadByte();
                unk00 = br.ReadByte();
                unk04 = br.ReadByte();
                unk08 = br.ReadByte();
                unk10 = br.ReadInt32();
                unk14 = br.ReadInt32();
                unk18 = br.AssertInt32(0);
                unk1C = br.AssertInt32(0);
                //br.Skip(12); //uninitialized memory
                uninitializedMemory = br.ReadBytes(12);
            }
        }
    }
}