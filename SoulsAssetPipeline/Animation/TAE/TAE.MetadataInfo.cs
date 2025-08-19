using SoulsFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoulsAssetPipeline.Animation
{
    public partial class TAE : SoulsFile<TAE>
    {

        /// <summary>
        /// Metadata info for various structs.
        /// </summary>
        public class MetadataInfo
        {
            public Guid GUID;

            public const string IndicatorString = "GUID=";

            internal static MetadataInfo ReadOptional(BinaryReaderEx br, TAEFormat format)
            {
                if ((br.Length - br.Position) >= IndicatorString.Length)
                {
                    var indicatorCheck = br.GetASCII(br.Position, IndicatorString.Length);
                    if (indicatorCheck == IndicatorString)
                    {
                        MetadataInfo result = new MetadataInfo();
                        var bytes = br.ReadBytes(16);
                        br.Pad(8);
                        result.GUID = new Guid(bytes);
                        return result;
                    }
                }
                return null;
            }

            internal void WriteOptional(BinaryWriterEx bw, TAEFormat format)
            {
                bw.WriteASCII(IndicatorString);
                bw.WriteBytes(GUID.ToByteArray());
            }
        }

    }
}
