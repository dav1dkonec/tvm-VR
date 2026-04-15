using System.Numerics;
using TVMEditor.Structures;

namespace TVMEditor.Editing.SurfaceDeformation
{
    public interface ISurfaceDeformation
    {
        TriangleMesh DeformSurface(Vector3[] vertices, TVMEditor.Structures.Face[] faces, Vector3[] oldCenters, Vector3[] newCenters, int frameIndex, DualQuaternion[] transformations);
        DualQuaternion[] ComputeDeformations(Vector3[] vertices, TVMEditor.Structures.Face[] faces, Vector3[] oldCenters, Vector3[] newCenters, int frameIndex, DualQuaternion[] transformations);
    }
}
