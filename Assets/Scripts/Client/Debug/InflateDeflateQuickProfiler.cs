using System.IO;
using TvmVr2.Api.Enums;
using TvmVr2.Client.Sequence;
using UnityEngine;

namespace TvmVr2.Client.DebugTools
{
    [DisallowMultipleComponent]
    public sealed class InflateDeflateQuickProfiler : MonoBehaviour
    {
        private const float VisibleRadius = 0.30f;
        private const float VisibleStrength = 0.18f;

        [SerializeField] private global::Sequence sequence;
        [SerializeField] private bool useCurrentFrame = true;
        [SerializeField] private int frameIndex;
        [SerializeField] private int centerIndex;
        [SerializeField] private float radius = 0.20f;
        [SerializeField] private float strength = 0.10f;
        [SerializeField] private InflateDeflateMode mode = InflateDeflateMode.Inflate;
        [SerializeField] private int iterations = 1;
        [SerializeField] private bool useStreamingAssetsCache = true;

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
                iterations);
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
            ApplyVisibleDebugEdit(InflateDeflateMode.Inflate);
        }

        [ContextMenu("Apply Visible Deflate Debug Edit")]
        public void ApplyVisibleDeflateDebugEdit()
        {
            ApplyVisibleDebugEdit(InflateDeflateMode.Deflate);
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

        private void ApplyVisibleDebugEdit(InflateDeflateMode visibleMode)
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

            var resolvedCenterIndex = ResolveHighestYCenterIndex(targetFrameIndex);
            if (resolvedCenterIndex < 0)
            {
                Debug.LogError("InflateDeflateQuickProfiler: unable to resolve a visible center for debug apply.");
                return;
            }

            centerIndex = resolvedCenterIndex;
            Debug.Log(
                $"InflateDeflateQuickProfiler: applying visible debug edit " +
                $"frame={targetFrameIndex}, center={centerIndex}, radius={VisibleRadius:F2}, strength={VisibleStrength:F2}, mode={visibleMode}.");

            sequence.ApplyInflateDeflateDebug(
                targetFrameIndex,
                centerIndex,
                VisibleRadius,
                VisibleStrength,
                visibleMode);
        }

        private int ResolveHighestYCenterIndex(int targetFrameIndex)
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
            var bestIndex = 0;
            var bestY = centers[0].Y;
            for (var i = 1; i < centers.Length; i++)
            {
                if (centers[i].Y <= bestY)
                    continue;

                bestY = centers[i].Y;
                bestIndex = i;
            }

            return bestIndex;
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
