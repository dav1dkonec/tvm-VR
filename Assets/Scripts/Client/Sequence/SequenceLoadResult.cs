using System;
using TvmVr2.Api.Sequence;

namespace TvmVr2.Client.Sequence
{
    /// <summary>
    /// Result of sequence loading.
    /// </summary>
    public sealed class SequenceLoadResult
    {
        /// <summary>
        /// Whether loading succeeded.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message when loading failed.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Loaded runtime frames.
        /// </summary>
        public Frame[] Frames { get; set; } = Array.Empty<Frame>();

        /// <summary>
        /// Loaded sequence settings.
        /// </summary>
        public SequenceSettings Settings { get; set; } = new SequenceSettings();

        /// <summary>
        /// Loaded sequence data.
        /// </summary>
        public SequenceData SequenceData { get; set; }
    }
}
