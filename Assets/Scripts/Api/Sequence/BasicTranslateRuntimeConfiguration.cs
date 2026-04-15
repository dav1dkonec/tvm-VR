namespace TvmVr2.Api.Sequence
{
    public sealed class BasicTranslateRuntimeConfiguration
    {
        public float CenterSigma { get; set; } = 1f;
        public int SequenceNeighborCount { get; set; } = 4;
        public int SurfaceNeighborCount { get; set; } = 6;
    }
}
