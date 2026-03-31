using System.Numerics;

namespace CoreTvm
{
    public sealed class MeshFrameData
    {
        public Vector3[] Vertices { get; set; } = new Vector3[0];
        public FaceData[] Faces { get; set; } = new FaceData[0];
    }
}
