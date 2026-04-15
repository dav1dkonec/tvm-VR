using System.Numerics;
using TVMEditor.Structures;

namespace TVMEditor.Editing.CenterDeformation
{
    public interface ICenterDeformation
    {
        Vector3[] DeformCenters(Vector3[] centers, int[] centerIndices, Vector3[] newPositions, ref DualQuaternion[] transformations);
        Vector3[] DeformCenters2(Vector3[] centers, int[] centerIndices, Vector3[] newPositions, ref DualQuaternion[] dualQuaternions);
    }
}
