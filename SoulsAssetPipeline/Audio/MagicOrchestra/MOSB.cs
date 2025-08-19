using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SoulsFormats;

namespace SoulsAssetPipeline.Audio.MagicOrchestra
{
    public class MOSB : SoulsFile<MOSB>
    {
        public bool BigEndian { get; set; }
        public MOSBHeader Header { get; set; }
        public List<SoundDef> SoundDefs { get; set; }
        public List<Event> Events { get; set; }
        protected override void Read(BinaryReaderEx br)
        {
            BigEndian = true;
            br.BigEndian = BigEndian;

            Header = new MOSBHeader(br);

            br.StepIn(Header.soundDefsOffset);
            SoundDefs = new List<SoundDef>(Header.soundDefsCount);
            for (int i = 0; i < Header.soundDefsCount; i++)
                SoundDefs.Add(new SoundDef(br, Header));

            br.StepIn(Header.eventsOffset);
            Events = new List<Event>(Header.eventsCount);
            if (Header.eventsCount > 0)
            {
                for (int i = 0; i < Header.eventsCount; i++)
                    Events.Add(new Event(br, Header));
            }

            br.StepIn(Header.offset11);
            br.AssertASCII(Header.name);
        }

        public class MOSBHeader
        {
            public string magic { get; set; }
            public short unk04 { get; set; }
            public short unk06 { get; set; }
            internal int soundDefsCount { get; set; }
            internal int eventsCount { get; set; }
            public string name { get; set; }
            internal int soundDefsOffset { get; set; }
            internal int eventsOffset { get; set; }
            internal int offset3 { get; set; }
            internal int offset4 { get; set; }
            internal int offset5 { get; set; }
            internal int offset6 { get; set; }
            internal int offset7 { get; set; }
            internal int offset8 { get; set; }
            internal int offset9 { get; set; }
            internal int offset10 { get; set; }
            internal int offset11 { get; set; }
            internal int offset12 { get; set; }
            internal MOSBHeader(BinaryReaderEx br)
            {
                magic = br.AssertASCII("MOSB");
                unk04 = br.AssertInt16(2);
                unk06 = br.AssertInt16(0);
                soundDefsCount = br.ReadInt32();
                eventsCount = br.ReadInt32();
                name = br.ReadFixStr(0x20); // Longest name in practice is 0xC so this is a bit of a guess
                soundDefsOffset = br.ReadInt32();
                eventsOffset = br.ReadInt32();
                offset3 = br.ReadInt32();
                offset4 = br.ReadInt32();
                offset5 = br.ReadInt32();
                offset6 = br.ReadInt32();
                offset7 = br.ReadInt32();
                offset8 = br.ReadInt32();
                offset9 = br.ReadInt32();
                offset10 = br.ReadInt32();
                offset11 = br.ReadInt32();
                offset12 = br.ReadInt32();
            }
        }
        public class SoundDef
        {
            public float volume { get; set; }
            public float pitch { get; set; }
            public long offset { get; set; }
            public byte categoryID { get; set; }
            public short[] struct3s { get; set; } = new short[0];
            public List<Layer> Layers { get; set; } = new List<Layer>();
            internal SoundDef(BinaryReaderEx br, MOSBHeader header)
            {
                volume = br.ReadSingle();
                pitch = br.ReadSingle();
                categoryID = br.ReadByte();
                byte unk09 = br.AssertByte(0);
                byte struct3Count = br.ReadByte();
                byte layerCount = br.ReadByte();
                int struct3Offset = br.ReadInt32();
                int layerOffset = br.ReadInt32();

                if (struct3Count > 0)
                {
                    br.StepIn(header.offset3 + struct3Offset);
                    offset = br.Position;
                    struct3s = br.ReadInt16s(struct3Count);
                    br.StepOut();
                }

                br.StepIn(header.offset4 + layerOffset);
                Layers = new List<Layer>(layerCount);
                for (int i = 0; i < layerCount; i++)
                    Layers.Add(new Layer(br, header));
                br.StepOut();
            }
        }
        public class Layer
        {
            public Struct5 Struct5 { get; set; }
            internal Layer(BinaryReaderEx br, MOSBHeader header)
            {
                float unk00 = br.AssertSingle(1);
                byte unk04 = br.AssertByte(1);
                byte unk05 = br.AssertByte(0);
                byte unk06 = br.AssertByte(0);
                byte unk07 = br.AssertByte(0);
                int struct5Offset = br.ReadInt32();
                int unk0C = br.AssertInt32(0);
                int unk10 = br.AssertInt32(0);
                int unk14 = br.AssertInt32(0);

                br.StepIn(header.offset5 + struct5Offset);
                Struct5 = new Struct5(br, header);
                br.StepOut();
            }
        }
        public class Struct5
        {
            public short unk00 { get; set; }
            public List<Sound> Sounds { get; set; }
            internal Struct5(BinaryReaderEx br, MOSBHeader header)
            {
                unk00 = br.ReadInt16();
                short soundCount = br.ReadInt16();
                int unk04 = br.AssertInt32(0);
                int unk08 = br.AssertInt32(0);
                int unk0C = br.AssertInt32(0);
                int unk10 = br.AssertInt32(0);
                int soundOffset = br.ReadInt32();

                br.StepIn(header.offset6 + soundOffset);
                Sounds = new List<Sound>(soundCount);
                for (int i = 0; i < soundCount; i++)
                    Sounds.Add(new Sound(br));
                br.StepOut();
            }
        }
        public class Sound
        {
            public short soundID { get; set; }
            internal Sound(BinaryReaderEx br)
            {
                short unk00 = br.AssertInt16(0);
                short unk04 = br.AssertInt16(0);
                soundID = br.ReadInt16();
                short unk0C = br.AssertInt16(0);
            }
        }

        public class Event
        {
            public byte unk08 { get; set; }
            public byte unk09 { get; set; }
            public short unk0C { get; set; } // Byte or short?
            public string name { get; set; }
            public List<EventSoundDef> Defs { get; set; }
            internal Event(BinaryReaderEx br, MOSBHeader header)
            {
                int nameOffset = br.ReadInt32();
                byte defCount = br.ReadByte();
                byte unk05 = br.AssertByte(0);
                byte unk06 = br.AssertByte(0);
                byte unk07 = br.AssertByte(0);
                unk08 = br.ReadByte();
                unk09 = br.ReadByte();
                short unk0A = br.AssertInt16(0);
                unk0C = br.ReadInt16();/*AssertInt16(!(unk0C & 0xFF00));*/ // Byte or short?
                short unk0E = br.AssertInt16(0);
                int unk10 = br.AssertInt32(0);
                int unk14 = br.AssertInt32(0);
                int defOffset = br.ReadInt32();

                br.StepIn(header.offset12 + nameOffset);
                name = br.ReadShiftJIS();
                br.StepOut();

                br.StepIn(header.offset10 + defOffset);
                Defs = new List<EventSoundDef>(defCount);
                for (int i = 0; i < defCount; i++)
                    Defs.Add(new EventSoundDef(br));
                br.StepOut();
            }
        }
        public class EventSoundDef
        {
            public short soundDefID { get; set; }
            internal EventSoundDef(BinaryReaderEx br)
            {
                soundDefID = br.ReadInt16();
                short unk04 = br.AssertInt16(0);
                int unk08 = br.AssertInt32(0);
                int unk10 = br.AssertInt32(0);
            }
        }
    }
}
