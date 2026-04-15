using UnityEngine;
using TvmVr2.Api.Enums;
using TvmVr2.Core.Abstractions;

namespace TvmVr2.Core.Methods.BasicTranslate
{
    public sealed class BasicTranslateMethodInput : IMethodInput
    {
        public MethodKind MethodKind => MethodKind.BasicTranslate;
        public Frame[] Frames { get; set; }
        public int FrameIndex { get; set; }
        public int CenterIndex { get; set; }
        public Vector3 TargetPosition { get; set; }
        public float CenterSigma { get; set; }
        public int SequenceNeighborCount { get; set; }
        public int SurfaceNeighborCount { get; set; }
    }
}
