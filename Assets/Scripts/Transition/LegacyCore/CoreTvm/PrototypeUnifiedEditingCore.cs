using System;
using System.Numerics;
using System.Diagnostics;

namespace CoreTvm
{
    public sealed class PrototypeUnifiedEditingCore : IEditingCoreApi
    {
        private readonly IAffinityCalculator _affinityCalculator = new UnifiedAffinityCalculator();
        private readonly ICenterDeformationCalculator _centerDeformationCalculator = new UnifiedCenterDeformationCalculator();
        private readonly IPropagationCalculator _propagationCalculator = new UnifiedPropagationCalculator();
        private readonly ISurfaceDeformationCalculator _surfaceDeformationCalculator = new UnifiedSurfaceDeformationCalculator();

        public UnifiedProfilingSnapshot LastProfile { get; private set; } = new UnifiedProfilingSnapshot();

        public EditResult ApplyEdit(SequenceData input, EditRequest request, EditingOptions options)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            ValidateRequest(input, request);

            var totalStopwatch = Stopwatch.StartNew();

            var affinityStopwatch = Stopwatch.StartNew();
            var affinity = _affinityCalculator.Calculate(input.Centers, options);
            affinityStopwatch.Stop();

            var oldCenters = CloneCenters(input.Centers[request.FrameIndex]);

            var centerStopwatch = Stopwatch.StartNew();
            var newCenters = _centerDeformationCalculator.Deform(
                oldCenters,
                request.CenterIndices,
                request.NewCenterPositions,
                affinity,
                options);
            centerStopwatch.Stop();

            var propagationStopwatch = Stopwatch.StartNew();
            var propagation = _propagationCalculator.Propagate(
                input.Centers,
                request.FrameIndex,
                oldCenters,
                newCenters,
                affinity,
                options);
            propagationStopwatch.Stop();

            var surfaceStopwatch = Stopwatch.StartNew();
            var meshFrames = _surfaceDeformationCalculator.Deform(
                input.MeshFrames,
                input.Centers,
                propagation.Centers,
                propagation.AffectedFrames,
                affinity,
                options);
            surfaceStopwatch.Stop();

            totalStopwatch.Stop();

            LastProfile = new UnifiedProfilingSnapshot
            {
                TotalMilliseconds = (float)totalStopwatch.Elapsed.TotalMilliseconds,
                AffinityMilliseconds = (float)affinityStopwatch.Elapsed.TotalMilliseconds,
                CenterMilliseconds = (float)centerStopwatch.Elapsed.TotalMilliseconds,
                PropagationMilliseconds = (float)propagationStopwatch.Elapsed.TotalMilliseconds,
                SurfaceMilliseconds = (float)surfaceStopwatch.Elapsed.TotalMilliseconds
            };

            return new EditResult
            {
                Sequence = new SequenceData
                {
                    MeshFrames = meshFrames,
                    Centers = propagation.Centers,
                    Metadata = CloneMetadata(input.Metadata)
                },
                AffectedFrames = propagation.AffectedFrames
            };
        }

        public SequenceData RebuildSurface(SequenceData input, EditingOptions options)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            var totalStopwatch = Stopwatch.StartNew();
            var affinityStopwatch = Stopwatch.StartNew();
            var affinity = _affinityCalculator.Calculate(input.Centers, options);
            affinityStopwatch.Stop();

            var affectedFrames = new int[input.MeshFrames.Length];
            for (var i = 0; i < affectedFrames.Length; i++)
            {
                affectedFrames[i] = i;
            }

            var surfaceStopwatch = Stopwatch.StartNew();
            var meshFrames = _surfaceDeformationCalculator.Deform(
                input.MeshFrames,
                input.Centers,
                input.Centers,
                affectedFrames,
                affinity,
                options);
            surfaceStopwatch.Stop();
            totalStopwatch.Stop();

            LastProfile = new UnifiedProfilingSnapshot
            {
                TotalMilliseconds = (float)totalStopwatch.Elapsed.TotalMilliseconds,
                AffinityMilliseconds = (float)affinityStopwatch.Elapsed.TotalMilliseconds,
                CenterMilliseconds = 0f,
                PropagationMilliseconds = 0f,
                SurfaceMilliseconds = (float)surfaceStopwatch.Elapsed.TotalMilliseconds
            };

            return new SequenceData
            {
                MeshFrames = meshFrames,
                Centers = CloneCenters(input.Centers),
                Metadata = CloneMetadata(input.Metadata)
            };
        }

        private static void ValidateRequest(SequenceData input, EditRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.FrameIndex < 0 || request.FrameIndex >= input.Centers.Length)
                throw new ArgumentOutOfRangeException(nameof(request.FrameIndex));

            if (request.CenterIndices == null || request.NewCenterPositions == null)
                throw new ArgumentException("Edit request must contain edited center indices and positions.");

            if (request.CenterIndices.Length != request.NewCenterPositions.Length)
                throw new ArgumentException("Edited center indices and positions must have the same length.");
        }

        private static Vector3[][] CloneCenters(Vector3[][] centers)
        {
            var clone = new Vector3[centers.Length][];
            for (var i = 0; i < centers.Length; i++)
            {
                clone[i] = CloneCenters(centers[i]);
            }

            return clone;
        }

        private static Vector3[] CloneCenters(Vector3[] centers)
        {
            var clone = new Vector3[centers.Length];
            Array.Copy(centers, clone, centers.Length);
            return clone;
        }

        private static SequenceMetadata CloneMetadata(SequenceMetadata metadata)
        {
            return new SequenceMetadata
            {
                FrameRate = metadata?.FrameRate ?? 0,
                SourcePath = metadata?.SourcePath ?? string.Empty,
                SequenceName = metadata?.SequenceName ?? string.Empty
            };
        }
    }
}
