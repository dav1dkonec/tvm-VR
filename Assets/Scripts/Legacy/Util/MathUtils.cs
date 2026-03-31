using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Math helper methods
/// </summary>
public class MathUtils : MonoBehaviour
{
    /// <summary>
    /// Creates a random unit length vector
    /// </summary>
    /// <returns>A random unit length vector</returns>
    public static Vector3 RandomUnitVector3()
    {
        var x = Random.value * 2 - 1;
        var y = Random.value * 2 - 1;
        var z = Random.value * 2 - 1;
        return new Vector3(x, y, z).normalized;
    }

    /// <summary>
    /// Convert to 4-component vertices
    /// </summary>
    /// <param name="vertices">Original vertices</param>
    /// <returns>Vertices with 4 components</returns>
    public static System.Numerics.Vector4[] ToVec4(System.Numerics.Vector3[] vertices)
    {
        System.Numerics.Vector4[] mesh = new System.Numerics.Vector4[vertices.Length];

        for (int i = 0; i < vertices.Length; i++)
        {
            System.Numerics.Vector3 v = vertices[i];
            mesh[i] = new System.Numerics.Vector4(v.X, v.Y, v.Z, 1);
        }

        return mesh;
    }
}
