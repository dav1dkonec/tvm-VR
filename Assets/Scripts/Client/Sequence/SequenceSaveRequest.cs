using System;
using TvmVr2.Api.Sequence;

namespace TvmVr2.Client.Sequence
{
    /// <summary>
    /// Request for saving a sequence.
    /// </summary>
    public sealed class SequenceSaveRequest
    {
        /// <summary>
        /// Base output directory path.
        /// </summary>
        public string BaseDirectoryPath { get; set; } = string.Empty;

        /// <summary>
        /// Sequence name.
        /// </summary>
        public string SequenceName { get; set; } = string.Empty;

        /// <summary>
        /// Frames to save.
        /// </summary>
        public Frame[] Frames { get; set; } = Array.Empty<Frame>();

        /// <summary>
        /// Save timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
