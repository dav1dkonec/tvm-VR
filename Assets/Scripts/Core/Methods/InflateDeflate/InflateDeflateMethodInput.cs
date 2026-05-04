using TvmVr2.Api.Enums;
using TvmVr2.Core.Abstractions;

namespace TvmVr2.Core.Methods.InflateDeflate
{
    public sealed class InflateDeflateMethodInput : IMethodInput
    {
        public MethodKind MethodKind => MethodKind.InflateDeflate;
        public string SequenceId { get; set; } = string.Empty;
        public Frame[] Frames { get; set; }
        public Frame[] CacheFrames { get; set; }
        public int FrameIndex { get; set; }
        public int SelectedCenterIndex { get; set; } = -1;
        public float Radius { get; set; }
        public float Strength { get; set; }
        public InflateDeflateMode Mode { get; set; }
        public int[] SelectedCenterIndices { get; set; } = System.Array.Empty<int>();
        public System.Numerics.Vector3[] CenterTranslations { get; set; } = System.Array.Empty<System.Numerics.Vector3>();
    }
}
