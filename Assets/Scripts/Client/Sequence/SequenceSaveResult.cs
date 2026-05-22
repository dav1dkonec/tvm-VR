namespace TvmVr2.Client.Sequence
{
    /// <summary>
    /// Result of sequence saving.
    /// </summary>
    public sealed class SequenceSaveResult
    {
        /// <summary>
        /// Whether saving succeeded.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message when saving failed.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Output directory path.
        /// </summary>
        public string OutputDirectoryPath { get; set; } = string.Empty;
    }
}
