using System.Numerics;

namespace CoreTvm
{
    public interface IAffinityCalculator
    {
        float[,] Calculate(Vector3[][] centers, EditingOptions options);
    }
}
