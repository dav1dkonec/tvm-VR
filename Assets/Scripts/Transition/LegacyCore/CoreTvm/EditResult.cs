namespace CoreTvm
{
    public sealed class EditResult
    {
        public SequenceData Sequence { get; set; } = new SequenceData();
        public int[] AffectedFrames { get; set; } = new int[0];
    }
}
