using SoulsFormats;
using System.Collections.Generic;
using System.Numerics;

namespace HKX2
{
    public partial class hkcdStaticAabbTree : hkReferencedObject
    {
        public override uint Signature { get => 2582171851; }
        
        public hkcdStaticTreeDefaultTreeStorage6 m_treePtr;
        
        public override void Read(PackFileDeserializer des, BinaryReaderEx br)
        {
            base.Read(des, br);
            br.ReadUInt64();
            m_treePtr = des.ReadClassPointer<hkcdStaticTreeDefaultTreeStorage6>(br);
        }
        
        public override void Write(PackFileSerializer s, BinaryWriterEx bw)
        {
            base.Write(s, bw);
            bw.WriteUInt64(0);
            s.WriteClassPointer<hkcdStaticTreeDefaultTreeStorage6>(bw, m_treePtr);
        }
    }
}
