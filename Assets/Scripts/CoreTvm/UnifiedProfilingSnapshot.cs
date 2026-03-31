namespace CoreTvm
{
    public sealed class UnifiedProfilingSnapshot
    {
        public float TotalMilliseconds { get; set; }
        public float AffinityMilliseconds { get; set; }
        public float CenterMilliseconds { get; set; }
        public float PropagationMilliseconds { get; set; }
        public float SurfaceMilliseconds { get; set; }
    }
}
