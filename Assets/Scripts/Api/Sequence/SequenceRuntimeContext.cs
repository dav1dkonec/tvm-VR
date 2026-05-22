
namespace TvmVr2.Api.Sequence
{
    /// <summary>
    /// Mutable runtime context passed to editing methods.
    /// </summary>
    public sealed class SequenceRuntimeContext
    {
        /// <summary>
        /// Loaded sequence identifier.
        /// </summary>
        public string SequenceId { get; set; } = string.Empty;

        /// <summary>
        /// Current frame index.
        /// </summary>
        public int CurrentFrameIndex { get; set; }

        /// <summary>
        /// Number of runtime frames.
        /// </summary>
        public int FrameCount { get; set; }

        /// <summary>
        /// Number of centers in the current sequence.
        /// </summary>
        public int CenterCount { get; set; }

        /// <summary>
        /// Mutable edited frames.
        /// </summary>
        public Frame[] Frames { get; set; }

        /// <summary>
        /// Original frames used by cached computations.
        /// </summary>
        public Frame[] CacheFrames { get; set; }

        /// <summary>
        /// Loaded sequence display name.
        /// </summary>
        public string LoadedName { get; set; } = string.Empty;

        /// <summary>
        /// Basic translate runtime configuration.
        /// </summary>
        public BasicTranslateRuntimeConfiguration BasicTranslate { get; set; } = new BasicTranslateRuntimeConfiguration();

        /// <summary>
        /// Inflate/deflate runtime configuration.
        /// </summary>
        public InflateDeflateRuntimeConfiguration InflateDeflate { get; set; } = new InflateDeflateRuntimeConfiguration();
    }
}
