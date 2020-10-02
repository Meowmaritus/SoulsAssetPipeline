using Assimp;
using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoulsAssetPipeline
{
    public static class SapMath
    {
        public static System.Numerics.Vector3 XYZ(this System.Numerics.Vector4 v)
        {
            return new System.Numerics.Vector3(v.X, v.Y, v.Z);
        }

        public static float Lerp(float a, float b, float s)
        {
            return a + ((b - a) * s);
        }

        public static FLVER.VertexColor ToFlverVertexColor(this Color4D c)
        {
            return new FLVER.VertexColor(c.A, c.R, c.G, c.B);
        }

        public static System.Numerics.Vector3 ToNumerics(this Vector3D v)
        {
            return new System.Numerics.Vector3(v.X, v.Y, v.Z);
        }

        public static System.Numerics.Quaternion ToNumerics(this Quaternion q)
        {
            return new System.Numerics.Quaternion(q.X, q.Y, q.Z, q.W);
        }
    }
}
