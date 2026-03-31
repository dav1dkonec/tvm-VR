namespace CoreTvm
{
    public sealed class EditingOptions
    {
        public int AffinityNeighbors { get; set; } = 24;
        public float AffinityBlendAlpha { get; set; } = 0.5f;
        public float CenterBlendAlpha { get; set; } = 0.6f;
        public float GaussianSigma { get; set; } = 1f;
        public float PropagationBlendAlpha { get; set; } = 0.65f;
        public int PropagationNeighbors { get; set; } = 4;
        public float TimeAttenuationShape { get; set; } = 0.15f;
        public float AffectedFrameThreshold { get; set; } = 0.001f;
        public float SurfaceBlendAlpha { get; set; } = 0.55f;
        public int SurfaceNeighbors { get; set; } = 6;
        public float SurfaceShape { get; set; } = 2f;
        public float SurfaceEpsilon { get; set; } = 0.0001f;
        public bool FullSurfaceAfterEdit { get; set; } = false;
    }
}
