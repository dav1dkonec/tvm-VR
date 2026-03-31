using System.Numerics;

namespace CoreTvm
{
    public sealed class PropagationResult
    {
        public Vector3[][] Centers { get; set; } = new Vector3[0][];
        public int[] AffectedFrames { get; set; } = new int[0];
    }
}
