namespace TvmVr2.Client.Sequence
{
    /// <summary>
    /// Request for loading a sequence.
    /// </summary>
    public sealed class SequenceLoadRequest
    {
        /// <summary>
        /// Sequence directory path.
        /// </summary>
        public string SequencePath { get; set; } = string.Empty;

        /// <summary>
        /// Sequence display name.
        /// </summary>
        public string SequenceName { get; set; } = string.Empty;

        /// <summary>
        /// Number of nearest centers per vertex.
        /// </summary>
        public int NearestCenterCount { get; set; }
    }
}
