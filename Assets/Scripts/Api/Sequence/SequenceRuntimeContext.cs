
namespace TvmVr2.Api.Sequence
{
    public sealed class SequenceRuntimeContext
    {
        public string SequenceId { get; set; } = string.Empty;
        public int CurrentFrameIndex { get; set; }
        public int FrameCount { get; set; }
        public int CenterCount { get; set; }
        public Frame[] Frames { get; set; }
        public string LoadedName { get; set; } = string.Empty;
        public BasicTranslateRuntimeConfiguration BasicTranslate { get; set; } = new BasicTranslateRuntimeConfiguration();
        public InflateDeflateRuntimeConfiguration InflateDeflate { get; set; } = new InflateDeflateRuntimeConfiguration();
    }
}
