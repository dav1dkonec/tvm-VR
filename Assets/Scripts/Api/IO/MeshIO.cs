using System.Collections.Generic;
using System.IO;
using System.Numerics;

/// <summary>
/// Mesh input/output methods
/// </summary>
public class MeshIO
{
    /// <summary>
    /// Loads a mesh from an .obj file
    /// </summary>
    /// <param name="file">Path to .obj file</param>
    /// <param name="vertices">Array into which to load vertices</param>
    /// <param name="faces">Array into which to load faces</param>
    public static void LoadMesh(string file, out Vector3[] vertices, out Face[] faces)
    {
        List<Vector3> v = new();
        List<Face> f = new();

        using (StreamReader reader = new(OpenReadShared(file)))
        {
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine().Trim();

                if (line.StartsWith("v "))
                {
                    string[] split = line.Trim().Split(' ');
                    float x = float.Parse(split[1]);
                    float y = float.Parse(split[2]);
                    float z = float.Parse(split[3]);
                    var vec = new Vector3(x, y, z);
                    v.Add(vec);
                }
            }
        }

        using (StreamReader reader = new(OpenReadShared(file)))
        {
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine().Trim();

                if (line.StartsWith("f "))
                {
                    string[] split = line.Trim().Split(' ');

                    string[] splitx = split[1].Trim().Split('/');
                    string[] splity = split[2].Trim().Split('/');
                    string[] splitz = split[3].Trim().Split('/');

                    int v1 = int.Parse(splitx[0]) - 1;
                    int v2 = int.Parse(splity[0]) - 1;
                    int v3 = int.Parse(splitz[0]) - 1;

                    var face = new Face(v1, v2, v3);
                    f.Add(face);
                }
            }
        }

        vertices = v.ToArray();
        faces = f.ToArray();
    }

    /// <summary>
    /// Writes a mesh to a given location
    /// </summary>
    /// <param name="vertices">The vertices of the mesh</param>
    /// <param name="faces">The faces of the mesh</param>
    /// <param name="path">Where to write it to</param>
    public static void WriteMesh(Vector3[] vertices, Face[] faces, string path)
    {
        using StreamWriter sw = new(path, false);

        for (int i = 0; i < vertices.Length; i++)
        {
            var v = vertices[i];
            sw.WriteLine($"v {v.X} {v.Y} {v.Z}");
        }

        for (int i = 0; i < faces.Length; i++)
        {
            var f = faces[i];
            sw.WriteLine($"f {f.V1 + 1} {f.V2 + 1} {f.V3 + 1}");
        }
    }

    private static FileStream OpenReadShared(string file)
    {
        return new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    }

}
