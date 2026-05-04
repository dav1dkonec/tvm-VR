using System.IO;
using TvmVr2.Api.Enums;
using TvmVr2.Client.Sequence;
using UnityEngine;

namespace TvmVr2.Client.DebugTools
{
    [DisallowMultipleComponent]
    public sealed class InflateDeflateQuickProfiler : MonoBehaviour
    {
        [SerializeField] private global::Sequence sequence;
        [SerializeField] private bool useCurrentFrame = true;
        [SerializeField] private int frameIndex;
        [SerializeField] private int centerIndex;
        [SerializeField] private float radius = 0.20f;
        [SerializeField] private float strength = 0.10f;
        [SerializeField] private InflateDeflateMode mode = InflateDeflateMode.Inflate;
        [SerializeField] private int iterations = 1;
        [SerializeField] private bool useStreamingAssetsCache = true;
        [SerializeField] private bool restoreOriginalStateAfterQuickProfile = true;

        public enum DebugBodyRegion
        {
            Head,
            Belly,
            Arm,
            Leg
        }

        [ContextMenu("Run InflateDeflate Quick Profile")]
        public void RunQuickProfile()
        {
            if (!EnsureSequence())
                return;

            sequence.SetInflateDeflateStreamingCacheEnabled(useStreamingAssetsCache);
            var targetFrameIndex = useCurrentFrame ? sequence.currentFrame : frameIndex;
            sequence.RunInflateDeflateQuickProfile(
                targetFrameIndex,
                centerIndex,
                radius,
                strength,
                mode,
                iterations,
                restoreOriginalStateAfterQuickProfile);
        }

        [ContextMenu("Load Short Samba")]
        public void LoadShortSamba()
        {
            LoadSequenceByName("short_samba");
        }

        [ContextMenu("Export InflateDeflate Cache")]
        public void ExportInflateDeflateCache()
        {
            if (!EnsureSequence())
                return;

            if (!UnityEngine.Application.isPlaying)
            {
                Debug.LogError("InflateDeflateQuickProfiler: cache export is available only in Play Mode.");
                return;
            }

            sequence.ExportInflateDeflateCacheToStreamingAssets();
        }

        [ContextMenu("Apply Visible Inflate Debug Edit")]
        public void ApplyVisibleInflateDebugEdit()
        {
            ApplyRegionDebugEdit(DebugBodyRegion.Head, InflateDeflateMode.Inflate);
        }

        [ContextMenu("Apply Visible Deflate Debug Edit")]
        public void ApplyVisibleDeflateDebugEdit()
        {
            ApplyRegionDebugEdit(DebugBodyRegion.Head, InflateDeflateMode.Deflate);
        }

        public void MoveSequenceToCamera()
        {
            if (!EnsureSequence())
                return;

            if (!UnityEngine.Application.isPlaying)
            {
                Debug.LogError("InflateDeflateQuickProfiler: moving sequence to camera is available only in Play Mode.");
                return;
            }

            var cameraTransform = Camera.main != null
                ? Camera.main.transform
                : FindFirstObjectByType<Camera>()?.transform;
            if (cameraTransform == null)
            {
                Debug.LogError("InflateDeflateQuickProfiler: no camera was found.");
                return;
            }

            var sequenceTransform = sequence.transform;
            sequenceTransform.position = cameraTransform.position + cameraTransform.forward * 0.75f;
            sequenceTransform.rotation = Quaternion.LookRotation(sequenceTransform.position - cameraTransform.position, Vector3.up);
        }

        private bool EnsureSequence()
        {
            if (sequence == null)
                sequence = GetComponent<global::Sequence>() ?? FindFirstObjectByType<global::Sequence>();

            if (sequence != null)
                return true;

            Debug.LogWarning("InflateDeflateQuickProfiler: Sequence was not found.");
            return false;
        }

        public void ApplyRegionDebugEdit(DebugBodyRegion region, InflateDeflateMode visibleMode)
        {
            if (!EnsureSequence())
                return;

            if (!UnityEngine.Application.isPlaying)
            {
                Debug.LogError("InflateDeflateQuickProfiler: visible debug edit can be applied only in Play Mode.");
                return;
            }

            if (sequence.frames == null || sequence.frames.Length == 0)
            {
                Debug.LogError("InflateDeflateQuickProfiler: no loaded sequence is available for visible debug edit.");
                return;
            }

            var targetFrameIndex = useCurrentFrame ? sequence.currentFrame : frameIndex;
            if (targetFrameIndex < 0 || targetFrameIndex >= sequence.frames.Length || sequence.frames[targetFrameIndex] == null)
            {
                Debug.LogError($"InflateDeflateQuickProfiler: target frame {targetFrameIndex} is not available.");
                return;
            }

            var resolvedCenterIndex = ResolveRegionCenterIndex(targetFrameIndex, region);
            if (resolvedCenterIndex < 0)
            {
                Debug.LogError($"InflateDeflateQuickProfiler: unable to resolve a center for debug region {region}.");
                return;
            }

            centerIndex = resolvedCenterIndex;
            Debug.Log(
                $"InflateDeflateQuickProfiler: applying region debug edit " +
                $"frame={targetFrameIndex}, center={centerIndex}, region={region}, radius={radius:F2}, strength={strength:F2}, mode={visibleMode}.");

            sequence.ApplyInflateDeflateDebug(
                targetFrameIndex,
                centerIndex,
                radius,
                strength,
                visibleMode);
        }

        private int ResolveRegionCenterIndex(int targetFrameIndex, DebugBodyRegion region)
        {
            if (sequence == null ||
                sequence.frames == null ||
                targetFrameIndex < 0 ||
                targetFrameIndex >= sequence.frames.Length ||
                sequence.frames[targetFrameIndex] == null ||
                sequence.frames[targetFrameIndex].centers == null ||
                sequence.frames[targetFrameIndex].centers.Length == 0)
            {
                return -1;
            }

            var centers = sequence.frames[targetFrameIndex].centers;
            var minY = centers[0].Y;
            var maxY = centers[0].Y;
            var minX = centers[0].X;
            var maxX = centers[0].X;

            for (var i = 1; i < centers.Length; i++)
            {
                if (centers[i].Y < minY)
                    minY = centers[i].Y;
                if (centers[i].Y > maxY)
                    maxY = centers[i].Y;
                if (centers[i].X < minX)
                    minX = centers[i].X;
                if (centers[i].X > maxX)
                    maxX = centers[i].X;
            }

            var bodyCenterX = 0.5f * (minX + maxX);
            var ySpan = Mathf.Max(1e-4f, maxY - minY);
            var xSpan = Mathf.Max(1e-4f, maxX - minX);

            var bestIndex = -1;
            var bestScore = float.NegativeInfinity;

            for (var i = 0; i < centers.Length; i++)
            {
                var center = centers[i];
                var normalizedY = (center.Y - minY) / ySpan;
                var normalizedSide = Mathf.Abs(center.X - bodyCenterX) / xSpan;
                var score = EvaluateRegionScore(region, normalizedY, normalizedSide, center.X - bodyCenterX);
                if (score <= bestScore)
                    continue;

                bestScore = score;
                bestIndex = i;
            }

            return bestIndex;
        }

        public bool ShouldRestoreOriginalStateAfterQuickProfile => restoreOriginalStateAfterQuickProfile;

        private static float EvaluateRegionScore(DebugBodyRegion region, float normalizedY, float normalizedSide, float signedSideOffset)
        {
            switch (region)
            {
                case DebugBodyRegion.Head:
                    return normalizedY * 3f - Mathf.Abs(signedSideOffset) * 0.05f;
                case DebugBodyRegion.Belly:
                    return (1f - Mathf.Abs(normalizedY - 0.52f)) * 2.5f - normalizedSide;
                case DebugBodyRegion.Arm:
                    return normalizedSide * 2.5f + Mathf.Clamp01(normalizedY - 0.35f) - Mathf.Abs(normalizedY - 0.62f);
                case DebugBodyRegion.Leg:
                    return normalizedSide * 1.5f + (1f - normalizedY) * 2.5f;
                default:
                    return float.NegativeInfinity;
            }
        }

        public bool LoadSequenceByName(string targetSequenceName)
        {
            if (!EnsureSequence())
                return false;

            if (!UnityEngine.Application.isPlaying)
            {
                Debug.LogError($"InflateDeflateQuickProfiler: {targetSequenceName} can be loaded only in Play Mode.");
                return false;
            }

            if (!TryResolveSequenceByName(targetSequenceName, out var sequencePath, out var sequenceName))
            {
                Debug.LogError($"InflateDeflateQuickProfiler: {targetSequenceName} was not found in the configured sequence catalog.");
                return false;
            }

            sequence.SetInflateDeflateStreamingCacheEnabled(useStreamingAssetsCache);
            Debug.Log($"InflateDeflateQuickProfiler: loading {sequenceName} from {sequencePath}");
            sequence.Load(sequencePath, sequenceName);
            return true;
        }

        public static string[] GetCatalogSequenceNames()
        {
            var catalog = new SequenceCatalog();
            var settings = catalog.LoadOrCreateSettings("/settings.xml");
            var directories = catalog.GetSequenceDirectories(settings);
            var names = new string[directories.Length];

            for (var i = 0; i < directories.Length; i++)
                names[i] = Path.GetFileName(directories[i]);

            return names;
        }

        private static bool TryResolveSequenceByName(string targetSequenceName, out string sequencePath, out string sequenceName)
        {
            sequencePath = null;
            sequenceName = null;

            var catalog = new SequenceCatalog();
            var settings = catalog.LoadOrCreateSettings("/settings.xml");
            var directories = catalog.GetSequenceDirectories(settings);
            for (var i = 0; i < directories.Length; i++)
            {
                var candidatePath = directories[i];
                var candidateName = Path.GetFileName(candidatePath);
                if (!string.Equals(candidateName, targetSequenceName, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                sequencePath = candidatePath;
                sequenceName = candidateName;
                return true;
            }

            return false;
        }
    }
}
