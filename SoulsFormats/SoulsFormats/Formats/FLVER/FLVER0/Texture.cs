namespace SoulsFormats
{
    public partial class FLVER0
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public class Texture : IFlverTexture
        {
            public string Type { get; set; }

            public string Path { get; set; }

            internal Texture(BinaryReaderEx br, FLVER0 flv)
            {
                long pathOffset = br.ReadVarint();
                long typeOffset = br.ReadVarint();
                if (!br.VarintLong)
                {
                    br.AssertInt32(0);
                    br.AssertInt32(0);
                }

                Path = flv.Unicode ? br.GetUTF16(pathOffset) : br.GetShiftJIS(pathOffset);
                if (typeOffset > 0)
                    Type = flv.Unicode ? br.GetUTF16(typeOffset) : br.GetShiftJIS(typeOffset);
                else
                    Type = null;
            }
        }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
}
