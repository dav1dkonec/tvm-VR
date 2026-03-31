using System.Numerics;

namespace CoreTvm
{
    public interface ICenterDeformationCalculator
    {
        Vector3[] Deform(Vector3[] centers, int[] centerIndices, Vector3[] newPositions, float[,] affinity, EditingOptions options);
    }
}
