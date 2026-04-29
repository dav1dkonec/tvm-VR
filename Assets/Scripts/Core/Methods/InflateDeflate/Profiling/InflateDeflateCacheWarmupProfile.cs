using System.Text;

namespace TvmVr2.Core.Methods.InflateDeflate.Profiling
{
    public sealed class InflateDeflateCacheWarmupProfile
    {
        public int FrameCount { get; set; }
        public int MaxDegreeOfParallelism { get; set; }
        public double AffinityMs { get; set; }
        public double SurfaceWeightsMs { get; set; }
        public double KabschNeighborsMs { get; set; }
        public double TotalMs { get; set; }
        public bool Success { get; set; }
        public bool WasCanceled { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;

        public string ToLogString(string header)
        {
            var builder = new StringBuilder();
            builder.AppendLine(header);
            builder.AppendLine($"FrameCount: {FrameCount}");
            builder.AppendLine($"MaxDegreeOfParallelism: {MaxDegreeOfParallelism}");
            builder.AppendLine($"Affinity: {AffinityMs:F2} ms");
            builder.AppendLine($"SurfaceWeights: {SurfaceWeightsMs:F2} ms");
            builder.AppendLine($"KabschNeighbors: {KabschNeighborsMs:F2} ms");
            builder.AppendLine($"Total: {TotalMs:F2} ms");
            builder.AppendLine($"Canceled: {WasCanceled}");

            if (!Success && !string.IsNullOrWhiteSpace(ErrorMessage))
                builder.AppendLine($"Error: {ErrorMessage}");

            return builder.ToString();
        }
    }
}
