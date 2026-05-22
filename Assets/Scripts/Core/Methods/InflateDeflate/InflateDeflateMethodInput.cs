using TvmVr2.Api.Enums;
using TvmVr2.Core.Abstractions;

namespace TvmVr2.Core.Methods.InflateDeflate
{
    /// <summary>
    /// Core input for inflate/deflate.
    /// </summary>
    public sealed class InflateDeflateMethodInput : IMethodInput
    {
        /// <summary>
        /// Target editing method.
        /// </summary>
        public MethodKind MethodKind => MethodKind.InflateDeflate;

        /// <summary>
        /// Edited sequence identifier.
        /// </summary>
        public string SequenceId { get; set; } = string.Empty;

        /// <summary>
        /// Mutable edited frames.
        /// </summary>
        public Frame[] Frames { get; set; }

        /// <summary>
        /// Original frames used by cached computations.
        /// </summary>
        public Frame[] CacheFrames { get; set; }

        /// <summary>
        /// Edited frame index.
        /// </summary>
        public int FrameIndex { get; set; }

        /// <summary>
        /// User selected reference center index.
        /// </summary>
        public int SelectedCenterIndex { get; set; } = -1;

        /// <summary>
        /// Deformation strength.
        /// </summary>
        public float Strength { get; set; }

        /// <summary>
        /// Inflate or deflate mode.
        /// </summary>
        public InflateDeflateMode Mode { get; set; }

        /// <summary>
        /// Resolved effector center indices.
        /// </summary>
        public int[] SelectedCenterIndices { get; set; } = System.Array.Empty<int>();

        /// <summary>
        /// Resolved effector translations.
        /// </summary>
        public System.Numerics.Vector3[] CenterTranslations { get; set; } = System.Array.Empty<System.Numerics.Vector3>();
    }
}
