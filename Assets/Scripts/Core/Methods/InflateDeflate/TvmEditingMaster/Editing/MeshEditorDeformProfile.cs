namespace TVMEditor.Editing
{
    public sealed class MeshEditorDeformProfile
    {
        public double CloneInputsMs { get; set; }
        public double ResolveNewPositionsMs { get; set; }
        public double AffinityMs { get; set; }
        public double CenterDeformationMs { get; set; }
        public double EditedSurfaceMs { get; set; }
        public double PropagateTransformsMs { get; set; }
        public double PropagateSurfaceMs { get; set; }
        public double PropagateTotalMs { get; set; }
        public int PropagatedSurfaceFrames { get; set; }
        public double SurfaceEditedComputeWeightsMs { get; set; }
        public double SurfaceEditedBlendVerticesMs { get; set; }
        public double SurfaceEditedResampleMs { get; set; }
        public bool SurfaceEditedUsedCachedWeights { get; set; }
        public double SurfacePropagatedComputeWeightsMs { get; set; }
        public double SurfacePropagatedBlendVerticesMs { get; set; }
        public double SurfacePropagatedResampleMs { get; set; }
        public int SurfacePropagatedCacheMisses { get; set; }
        public int SurfacePropagatedCacheHits { get; set; }
        public double SurfacePropagatedMinCallTotalMs { get; set; }
        public double SurfacePropagatedMaxCallTotalMs { get; set; }
        public int SurfacePropagatedMaxCallFrameIndex { get; set; }
        public double SurfacePropagatedAverageCallTotalMs { get; set; }
        public double TotalMs { get; set; }
    }
}
