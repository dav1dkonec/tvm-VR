using UnityEngine;

namespace TvmVr2.Client.Mesh
{
    /// <summary>
    /// Presents sequence mesh frames.
    /// </summary>
    public sealed class SequenceMeshPresenter
    {
        /// <summary>
        /// Redraws mesh from frame data.
        /// </summary>
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
