using System;
using TvmVr2.Api.Sequence;

namespace TvmVr2.Client.Sequence
{
    public sealed class SequenceLoadResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public Frame[] Frames { get; set; } = Array.Empty<Frame>();
        public SequenceSettings Settings { get; set; } = new SequenceSettings();
    }
}
