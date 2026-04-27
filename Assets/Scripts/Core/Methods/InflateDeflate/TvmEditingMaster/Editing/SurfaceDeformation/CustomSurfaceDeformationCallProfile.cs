namespace TVMEditor.Editing.SurfaceDeformation
{
    public sealed class CustomSurfaceDeformationCallProfile
    {
        public int FrameIndex { get; set; }
        public bool UsedCachedWeights { get; set; }
        public double ComputeWeightsMs { get; set; }
        public double BlendVerticesMs { get; set; }
        public double ResampleMs { get; set; }
        public double TotalMs { get; set; }
    }
}
