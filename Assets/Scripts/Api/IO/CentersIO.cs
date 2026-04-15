using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Numerics;

/// <summary>
/// Center file input/output methods
/// </summary>
public class CentersIO
{
    /// <summary>
    /// Loads all center files from the specified paths
    /// </summary>
    /// <param name="centersFiles">Paths to centers files</param>
    /// <returns>Loaded centers</returns>
    public static Vector3[][] LoadCentersFiles(string[] centersFiles)
    {
        var centers = new Vector3[centersFiles.Length][];

        Parallel.For(0, centersFiles.Length, i =>
        {
            centers[i] = LoadCentersFile(centersFiles[i]);
        });

        return centers;
    }

    /// <summary>
    /// Loads one centers file
    /// </summary>
    /// <param name="centersFile">Path to file</param>
    /// <returns>Loaded centers</returns>
    public static Vector3[] LoadCentersFile(string centersFile)
    {
        if (centersFile.EndsWith(".bin"))
        {
            return LoadBin(centersFile);
        }
        else if (centersFile.EndsWith(".xyz"))
        {
            return LoadXYZ(centersFile);
        }

        return null;
    }

    /// <summary>
    /// Loads a centers file in .xyz format
    /// </summary>
    /// <param name="file">Path to file</param>
    /// <returns>Loaded centers</returns>
    public static Vector3[] LoadXYZ(string file)
    {
        List<Vector3> r = new List<Vector3>();
        using (StreamReader reader = new StreamReader(new FileStream(file, FileMode.Open)))
        {
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                string[] split = line.Split(' ');
                float x = float.Parse(split[0]);
                float y = float.Parse(split[1]);
                float z = float.Parse(split[2]);
                var vec = new Vector3(x, y, z);
                r.Add(vec);
            }
        }
        return r.ToArray();
    }

    /// <summary>
    /// Writes centers to an .xyz file
    /// </summary>
    /// <param name="file">Path to file</param>
    /// <param name="centers">Centers to write to file</param>
    public static void WriteXYZ(string file, Vector3[] centers)
    {
        using (StreamWriter writer = new StreamWriter(file))
        {
            for (int i = 0; i < centers.Length; i++)
            {
                var c = centers[i];
                writer.WriteLine($"{c.X} {c.Y} {c.Z}");
            }
        }
    }

    /// <summary>
    /// Loads a centers file in .bin format
    /// </summary>
    /// <param name="file">Path to file</param>
    /// <returns>Loaded centers</returns>
    public static Vector3[] LoadBin(string file)
    {
        BinaryReader br = new BinaryReader(new FileStream(file, FileMode.Open));
        int n = br.ReadInt32();
        Vector3[] r = new Vector3[n];

        for (int i = 0; i < n; i++)
        {
            float x = br.ReadSingle();
            float y = br.ReadSingle();
            float z = br.ReadSingle();
            r[i] = new Vector3(x, y, z);
        }
        br.Close();
        
        return r;
    }
}
