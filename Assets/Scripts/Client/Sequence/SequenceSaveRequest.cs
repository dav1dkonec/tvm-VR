using System;
using TvmVr2.Api.Sequence;

namespace TvmVr2.Client.Sequence
{
    public sealed class SequenceSaveRequest
    {
        public string BaseDirectoryPath { get; set; } = string.Empty;
        public string SequenceName { get; set; } = string.Empty;
        public Frame[] Frames { get; set; } = Array.Empty<Frame>();
        public DateTime Timestamp { get; set; }
    }
}
