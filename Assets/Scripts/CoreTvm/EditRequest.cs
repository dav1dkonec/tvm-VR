using System.Numerics;

namespace CoreTvm
{
    public sealed class EditRequest
    {
        public int FrameIndex { get; set; }
        public int[] CenterIndices { get; set; } = new int[0];
        public Vector3[] NewCenterPositions { get; set; } = new Vector3[0];
    }
}
