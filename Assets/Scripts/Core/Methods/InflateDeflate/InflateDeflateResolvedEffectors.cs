using System.Numerics;

namespace TvmVr2.Core.Methods.InflateDeflate
{
    /// <summary>
    /// Resolved inflate/deflate effectors.
    /// </summary>
    public sealed class InflateDeflateResolvedEffectors
    {
        /// <summary>
        /// Effector center indices.
        /// </summary>
        public int[] Indices { get; set; } = System.Array.Empty<int>();

        /// <summary>
        /// Effector translations.
        /// </summary>
        public Vector3[] Translations { get; set; } = System.Array.Empty<Vector3>();
    }
}
