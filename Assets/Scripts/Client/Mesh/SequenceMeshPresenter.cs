using UnityEngine;

namespace TvmVr2.Client.Mesh
{
    public sealed class SequenceMeshPresenter
    {
        public void Redraw(UnityEngine.Mesh mesh, Frame frame)
        {
            if (mesh == null || frame == null)
                return;

            mesh.Clear();
            mesh.vertices = frame.GetUnityVertices();
            mesh.triangles = frame.GetUnityFaces();
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
        }
    }
}
