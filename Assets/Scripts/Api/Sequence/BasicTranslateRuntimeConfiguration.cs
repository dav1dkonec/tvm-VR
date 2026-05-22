namespace TvmVr2.Api.Sequence
{
    /// <summary>
    /// Runtime configuration for basic translate.
    /// </summary>
    public sealed class BasicTranslateRuntimeConfiguration
    {
        /// <summary>
        /// Gaussian falloff sigma for center deformation.
        /// </summary>
        public float CenterSigma { get; set; } = 1f;

        /// <summary>
        /// Number of temporal sequence neighbors.
        /// </summary>
        public int SequenceNeighborCount { get; set; } = 4;

        /// <summary>
        /// Number of surface deformation neighbors.
        /// </summary>
        public int SurfaceNeighborCount { get; set; } = 6;
    }
}
