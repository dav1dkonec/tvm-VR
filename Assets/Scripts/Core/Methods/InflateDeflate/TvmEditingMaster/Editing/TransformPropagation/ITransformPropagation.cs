using System.Numerics;
using TVMEditor.Structures;

namespace TVMEditor.Editing.TransformPropagation
{
    public interface ITransformPropagation
    {
        Vector3[][] PropagateTransform(Vector3[][] centers, int frameIndex, Vector3[] oldCenters, Vector3[] newCenters, DualQuaternion[] transformations, out DualQuaternion[][] propagatedTransformations);
        bool FrameIsAffected(int editedFrameIndex, int frameIndex);
    }
}
