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

        [ContextMenu("Run InflateDeflate Quick Profile")]
        public void RunQuickProfile()
        {
            if (!EnsureSequence())
                return;

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
            if (!EnsureSequence())
                return;

            if (!UnityEngine.Application.isPlaying)
            {
                Debug.LogError("InflateDeflateQuickProfiler: short_samba can be loaded only in Play Mode.");
                return;
            }

            if (!TryResolveShortSamba(out var sequencePath, out var sequenceName))
            {
                Debug.LogError("InflateDeflateQuickProfiler: short_samba was not found in the configured sequence catalog.");
                return;
            }

            Debug.Log($"InflateDeflateQuickProfiler: loading {sequenceName} from {sequencePath}");
            sequence.Load(sequencePath, sequenceName);
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

        private static bool TryResolveShortSamba(out string sequencePath, out string sequenceName)
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
                if (!string.Equals(candidateName, "short_samba", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                sequencePath = candidatePath;
                sequenceName = candidateName;
                return true;
            }

            return false;
        }
    }
}
