using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoulsAssetPipeline.Animation
{
    public class KHAQuantizedAnimationData
    {


        public void Read(BinaryReaderEx br)
        {
            ushort headerSize = br.ReadUInt16();

            ushort numBones = br.ReadUInt16();
            ushort numFloats = br.ReadUInt16();
            ushort numFrames = br.ReadUInt16();
            float duration = br.ReadSingle();

            ushort numStaticTranslations = br.ReadUInt16();
            ushort numStaticRotations = br.ReadUInt16();
            ushort numStaticScales = br.ReadUInt16();
            ushort numStaticFloats = br.ReadUInt16();

            ushort numDynamicTranslations = br.ReadUInt16();
            ushort numDynamicRotations = br.ReadUInt16();
            ushort numDynamicScales = br.ReadUInt16();
            ushort numDynamicFloats = br.ReadUInt16();

            ushort frameDataSize = br.ReadUInt16();

            ushort staticElementsOffset = br.ReadUInt16();
            ushort dynamicElementsOffset = br.ReadUInt16();

            ushort staticValuesOffset = br.ReadUInt16();
            ushort dynamicRangeMinimumsOffset = br.ReadUInt16();
            ushort dynamicRangeSpansOffset = br.ReadUInt16();


        }

    }
}
