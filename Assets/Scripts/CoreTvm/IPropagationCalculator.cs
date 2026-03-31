using System.Numerics;

namespace CoreTvm
{
    public interface IPropagationCalculator
    {
        PropagationResult Propagate(Vector3[][] centers, int editedFrameIndex, Vector3[] oldCenters, Vector3[] newCenters, float[,] affinity, EditingOptions options);
    }
}
