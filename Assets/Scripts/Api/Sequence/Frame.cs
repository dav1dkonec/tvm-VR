using System;
using System.Collections.Generic;
using System.Numerics;

/// <summary>
/// Represents one frame of the sequence
/// </summary>
public class Frame
{   
    /// <summary>
    /// Volume elements in this frame prior to any editing
    /// </summary>
    public Vector3[] centersUnedited;

    /// <summary>
    /// Volume elements in this frame
    /// </summary>
    public Vector3[] centers;

    /// <summary>
    /// Mesh vertices in this frame
    /// </summary>
    public Vector3[] verticesUnedited;

    /// <summary>
    /// Mesh vertices in this frame
    /// </summary>
    public Vector3[] vertices;

    /// <summary>
    /// Mesh connectivity in this frame
    /// </summary>
    public Face[] faces;

    /// <summary>
    /// Table of nearest center distances to each vertex
    /// </summary>
    public float[][] nearestCentersDist;

    /// <summary>
    /// Table of nearest centers to each vertex
    /// </summary>
    public int[][] nearestCentersIndex;

    /// <summary>
    /// Returns the mesh vertices as UnityEngine.Vector3s
    /// </summary>
    /// <returns>Vertex array as UnityEngine.Vector3 objects</returns>
    public UnityEngine.Vector3[] GetUnityVertices()
    {
        var verticesU = new UnityEngine.Vector3[vertices.Length];

        for (var i = 0; i < vertices.Length; i++)
        {
            verticesU[i] = new UnityEngine.Vector3(
                vertices[i].X,
                vertices[i].Y,
                vertices[i].Z);
        }

        return verticesU;
    }

    /// <summary>
    /// Returns mesh connectivity in the format used by Unity
    /// </summary>
    /// <returns>Mesh connectivity in the format used by Unity</returns>
    public int[] GetUnityFaces()
    {
        var facesU = new int[faces.Length * 3];

        for (var i = 0; i < faces.Length; i++)
        {
            facesU[3 * i + 0] = faces[i].V1;
            facesU[3 * i + 1] = faces[i].V2;
            facesU[3 * i + 2] = faces[i].V3;
        }

        return facesU;
    }

    /// <summary>
    /// Finds nearest centers for each mesh vertex.
    /// </summary>
    /// <param name="n">Number of nearest centers.</param>
    public void FindNearest(int n)
    {
        nearestCentersDist = new float[vertices.Length][];
        nearestCentersIndex = new int[vertices.Length][];

        // Repeat search until a sufficient number of neighbors is found
        var kdTree = new KDTree(centersUnedited);

        for (int i = 0; i < vertices.Length; i++)
        {
            var maxSearchDistance = 0.1f;
            var indices = kdTree.findAllCloserThan(vertices[i], maxSearchDistance);

            while (indices.Count < n)
            {
                maxSearchDistance *= 2;
                indices = kdTree.findAllCloserThan(vertices[i], maxSearchDistance);
            }

            // Sort neighbors
            var distances = new List<float>();
            for (int j = 0; j < indices.Count; j++)
            {
                distances.Add(Vector3.Distance(vertices[i], centersUnedited[indices[j]]));
            }

            var distancesArr = distances.ToArray();
            var indicesArr = indices.ToArray();
            Array.Sort(distancesArr, indicesArr);

            nearestCentersDist[i] = distancesArr[0..n];
            nearestCentersIndex[i] = indicesArr[0..n];
        }


    }
}
