using MathNet.Numerics.LinearAlgebra.Single;
using System;
using System.Numerics;

namespace TVMEditor.Extensions
{
    public static class VectorExtensions
    {
        public static Vector3 ToVector3(this DenseVector vector)
        {
            return new Vector3(vector[0], vector[1], vector[2]);
        }

        public static Vector4[] ToVector4(this Vector3[] vertices)
        {
            var mesh = new Vector4[vertices.Length];
            for (var i = 0; i < vertices.Length; i++)
            {
                var v = vertices[i];
                mesh[i] = new Vector4(v.X, v.Y, v.Z, 1);
            }

            return mesh;
        }

        public static Vector4 GetColumn(this Matrix4x4 m, int col)
        {
            if (col == 0) return new Vector4(m.M11, m.M21, m.M31, m.M41);
            if (col == 1) return new Vector4(m.M12, m.M22, m.M32, m.M42);
            if (col == 2) return new Vector4(m.M13, m.M23, m.M33, m.M43);
            if (col == 3) return new Vector4(m.M14, m.M24, m.M34, m.M44);
            throw new IndexOutOfRangeException();
        }

        public static Vector4 GetRow(this Matrix4x4 m, int row)
        {
            if (row == 0) return new Vector4(m.M11, m.M12, m.M13, m.M14);
            if (row == 1) return new Vector4(m.M21, m.M22, m.M23, m.M24);
            if (row == 2) return new Vector4(m.M31, m.M32, m.M33, m.M34);
            if (row == 3) return new Vector4(m.M41, m.M42, m.M43, m.M44);
            throw new IndexOutOfRangeException();
        }
    }
}
