using System.Numerics;

namespace TVMEditor.Editing.AffinityCalculation
{
    public interface IAffinityCalculation
    {
        float[,] CalculateCentersAffinity(Vector3[][] volumeCenters);
        float[,] GetCentersAffinity();
        void SetCentersAffinity(float[,] affinity);
    }
}
