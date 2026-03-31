using System.Numerics;

namespace CoreTvm
{
    public sealed class SequenceData
    {
        public MeshFrameData[] MeshFrames { get; set; } = new MeshFrameData[0];
        public Vector3[][] Centers { get; set; } = new Vector3[0][];
        public SequenceMetadata Metadata { get; set; } = new SequenceMetadata();
    }
}
