using System.Numerics;

namespace CoreTvm
{
    public interface ISurfaceDeformationCalculator
    {
        MeshFrameData[] Deform(MeshFrameData[] meshFrames, Vector3[][] oldCenters, Vector3[][] newCenters, int[] affectedFrames, float[,] affinity, EditingOptions options);
    }
}
