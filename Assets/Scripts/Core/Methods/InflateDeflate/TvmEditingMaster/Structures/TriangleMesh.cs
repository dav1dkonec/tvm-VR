using System.Numerics;

namespace TVMEditor.Structures
{
    public class TriangleMesh
    {
        public Vector3[] Vertices { get; set; }
        public TVMEditor.Structures.Face[] Faces { get; set; }

        public TriangleMesh Clone()
        {
            return new TriangleMesh
            {
                Vertices = (Vector3[])Vertices.Clone(),
                Faces = (TVMEditor.Structures.Face[])Faces.Clone()
            };
        }
    }
}
