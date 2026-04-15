namespace TvmVr2.Client.Sequence
{
    public sealed class SequenceSaveResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string OutputDirectoryPath { get; set; } = string.Empty;
    }
}
