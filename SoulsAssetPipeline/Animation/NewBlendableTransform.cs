using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SoulsAssetPipeline.Animation
{
    public struct NewBlendableTransform
    {
        public Matrix4x4 ComposedMatrix;

        public Vector3 Translation;
        public Vector3 Scale;
        public Quaternion Rotation;

        public static NewBlendableTransform Normalize(NewBlendableTransform v)
        {
            v.Rotation = Quaternion.Normalize(v.Rotation);
            return v;
        }

        public NewBlendableTransform Normalized()
        {
            return Normalize(this);
        }

        public static NewBlendableTransform operator *(NewBlendableTransform a, float b)
        {
            return new NewBlendableTransform()
            {
                Translation = a.Translation * b,
                Rotation = new Quaternion(a.Rotation.X * b, a.Rotation.Y * b, a.Rotation.Z * b, a.Rotation.W * b),
                Scale = a.Scale * b,
            };
        }

        public static NewBlendableTransform operator /(NewBlendableTransform a, float b)
        {
            return new NewBlendableTransform()
            {
                Translation = a.Translation / b,
                Rotation = new Quaternion(a.Rotation.X / b, a.Rotation.Y / b, a.Rotation.Z / b, a.Rotation.W / b),
                Scale = a.Scale / b,
            };
        }

        public static NewBlendableTransform operator *(NewBlendableTransform a, NewBlendableTransform b)
        {
            return new NewBlendableTransform()
            {
                Translation = a.Translation + b.Translation,
                Rotation = a.Rotation * b.Rotation,
                Scale = a.Scale * b.Scale,
            };
        }

        public static NewBlendableTransform operator /(NewBlendableTransform a, NewBlendableTransform b)
        {
            return new NewBlendableTransform()
            {
                Translation = a.Translation - b.Translation,
                Rotation = a.Rotation / b.Rotation,
                Scale = a.Scale / b.Scale,
            };
        }

        public static NewBlendableTransform operator +(NewBlendableTransform a, NewBlendableTransform b)
        {
            return new NewBlendableTransform()
            {
                Translation = a.Translation + b.Translation,
                Rotation = a.Rotation + b.Rotation,
                Scale = a.Scale + b.Scale,
            };
        }

        public NewBlendableTransform(Matrix4x4 matrix) : this()
        {
            ComposedMatrix = matrix;

            if (!Matrix4x4.Decompose(matrix, out Scale, out Rotation, out Translation))
            {
                var ex = new ArgumentException($"{nameof(matrix)} can't be decomposed", nameof(matrix));
                ex.Data.Add("matrix", matrix);
                throw ex;
            }
        }

        public static NewBlendableTransform Identity => new NewBlendableTransform()
        {
            Translation = Vector3.Zero,
            Rotation = Quaternion.Identity,
            Scale = Vector3.One,

            ComposedMatrix = Matrix4x4.Identity,
        };

        public static NewBlendableTransform Zero => new NewBlendableTransform()
        {
            Translation = Vector3.Zero,
            Rotation = new Quaternion(0, 0, 0, 0),
            Scale = Vector3.Zero,

            ComposedMatrix = new Matrix4x4(), // probably I dunno
        };

        public static NewBlendableTransform Lerp(NewBlendableTransform a, NewBlendableTransform b, float s)
        {
            return new NewBlendableTransform()
            {
                Translation = Vector3.Lerp(a.Translation, b.Translation, s),
                Scale = Vector3.Lerp(a.Scale, b.Scale, s),
                Rotation = Quaternion.Slerp(a.Rotation, b.Rotation, s),
            };
        }

        public Matrix4x4 GetMatrixScale()
        {
            return Matrix4x4.CreateScale(Scale);
        }

        public Matrix4x4 GetMatrix()
        {
            return

                Matrix4x4.CreateFromQuaternion(Quaternion.Normalize(Rotation)) *
                //Matrix4x4.CreateFromQuaternion(Rotation) *
                Matrix4x4.CreateTranslation(Translation);
        }

        public NewBlendableTransform Decomposed()
        {
            if (Matrix4x4.Decompose(ComposedMatrix, out Vector3 scale, out Quaternion rotation, out Vector3 translation))
            {
                Scale = scale;
                Rotation = rotation;
                Translation = translation;
            }
            else
            {
                //throw new Exception("REEEEEEE");
                Scale = Vector3.One;
                Translation = Vector3.Zero;
                Rotation = Quaternion.Identity;
            }

            return this;
        }

        public NewBlendableTransform Composed()
        {
            ComposedMatrix = GetMatrix();

            return this;
        }
    }

}
