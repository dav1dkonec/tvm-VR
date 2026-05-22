using UnityEngine;
using TvmVr2.Api.Enums;
using TvmVr2.Core.Abstractions;

namespace TvmVr2.Core.Methods.BasicTranslate
{
    /// <summary>
    /// Core input for basic translate.
    /// </summary>
    public sealed class BasicTranslateMethodInput : IMethodInput
    {
        /// <summary>
        /// Target editing method.
        /// </summary>
        public MethodKind MethodKind => MethodKind.BasicTranslate;

        /// <summary>
        /// Runtime sequence frames.
        /// </summary>
        public Frame[] Frames { get; set; }

        /// <summary>
        /// Edited frame index.
        /// </summary>
        public int FrameIndex { get; set; }

        /// <summary>
        /// Edited center index.
        /// </summary>
        public int CenterIndex { get; set; }

        /// <summary>
        /// Target center position.
        /// </summary>
        public Vector3 TargetPosition { get; set; }

        /// <summary>
        /// Gaussian center falloff sigma.
        /// </summary>
        public float CenterSigma { get; set; }

        /// <summary>
        /// Number of temporal neighbors.
        /// </summary>
        public int SequenceNeighborCount { get; set; }

        /// <summary>
        /// Number of surface neighbors.
        /// </summary>
        public int SurfaceNeighborCount { get; set; }
    }
}
