namespace TvmVr2.Client.Sequence
{
    public sealed class SequenceLoadRequest
    {
        public string SequencePath { get; set; } = string.Empty;
        public string SequenceName { get; set; } = string.Empty;
        public int NearestCenterCount { get; set; }
    }
}
