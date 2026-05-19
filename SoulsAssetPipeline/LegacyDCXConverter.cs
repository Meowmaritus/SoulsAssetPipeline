using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SoulsFormats;

namespace SoulsAssetPipeline
{
    public class LegacyDCXConverter
    {
        public enum LegacyDCXType
        {
            Unknown,
            None,
            Zlib,
            DCP_EDGE,
            DCP_DFLT,
            DCX_EDGE,
            DCX_DFLT_10000_24_9,
            DCX_DFLT_10000_44_9,
            DCX_DFLT_11000_44_8,
            DCX_DFLT_11000_44_9,
            DCX_DFLT_11000_44_9_15,
            DCX_KRAK,
            DCX_KRAK_MAX,
            DCX_ZSTD,
        }

        public static DCX.CompressionInfo ConvertLegacyDCXTypeToCompressionInfo(LegacyDCXType type)
        {
            switch (type)
            {
                case LegacyDCXType.None:
                    return new DCX.NoCompressionInfo();
                case LegacyDCXType.Zlib:
                    return new DCX.ZlibCompressionInfo();
                case LegacyDCXType.DCP_DFLT:
                    return new DCX.DcpDfltCompressionInfo();
                case LegacyDCXType.DCP_EDGE:
                    return new DCX.DcpEdgeCompressionInfo();
                case LegacyDCXType.DCX_EDGE:
                    return new DCX.DcxEdgeCompressionInfo();
                case LegacyDCXType.DCX_DFLT_10000_24_9:
                    return new DCX.DcxDfltCompressionInfo(DCX.DfltCompressionPreset.DCX_DFLT_10000_24_9);
                case LegacyDCXType.DCX_DFLT_10000_44_9:
                    return new DCX.DcxDfltCompressionInfo(DCX.DfltCompressionPreset.DCX_DFLT_10000_44_9);
                case LegacyDCXType.DCX_DFLT_11000_44_8:
                    return new DCX.DcxDfltCompressionInfo(DCX.DfltCompressionPreset.DCX_DFLT_11000_44_8);
                case LegacyDCXType.DCX_DFLT_11000_44_9:
                    return new DCX.DcxDfltCompressionInfo(DCX.DfltCompressionPreset.DCX_DFLT_11000_44_9);
                case LegacyDCXType.DCX_DFLT_11000_44_9_15:
                    return new DCX.DcxDfltCompressionInfo(DCX.DfltCompressionPreset.DCX_DFLT_11000_44_9_15);
                case LegacyDCXType.DCX_KRAK:
                    return new DCX.DcxKrakCompressionInfo(DCX.KrakCompressionPreset.EldenRing);
                case LegacyDCXType.DCX_KRAK_MAX:
                    return new DCX.DcxKrakCompressionInfo(DCX.KrakCompressionPreset.ArmoredCore6);
                case LegacyDCXType.DCX_ZSTD:
                    return new DCX.DcxZstdCompressionInfo(compressionLevel: 15);
                case LegacyDCXType.Unknown:
                default:
                    return new DCX.UnkCompressionInfo();
            }
        }
    }
}
