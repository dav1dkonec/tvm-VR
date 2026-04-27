using System.Numerics;
using TvmVr2.Api.Enums;
using TvmVr2.Core.Abstractions;

namespace TvmVr2.Core.Methods.InflateDeflate
{
    public sealed class InflateDeflateMethodInput : IMethodInput
    {
        public MethodKind MethodKind => MethodKind.InflateDeflate;
        public string SequenceId { get; set; } = string.Empty;
        public Frame[] Frames { get; set; }
        public int FrameIndex { get; set; }
        public Vector3 ReferencePoint { get; set; }
        public float Radius { get; set; }
        public float Strength { get; set; }
        public InflateDeflateMode Mode { get; set; }
        public int[] SelectedCenterIndices { get; set; } = System.Array.Empty<int>();
        public Vector3[] CenterTranslations { get; set; } = System.Array.Empty<Vector3>();
    }
}
