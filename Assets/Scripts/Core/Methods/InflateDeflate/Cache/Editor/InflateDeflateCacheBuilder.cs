using System;
using System.IO;
using TvmVr2.Client.Sequence;
using TvmVr2.Core.Methods.InflateDeflate;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TvmVr2.Core.Methods.InflateDeflate.Cache.Editor
{
    public sealed class InflateDeflateCacheBuilder : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var catalog = new SequenceCatalog();
            var settings = catalog.LoadOrCreateSettings("/settings.xml");
            var directories = catalog.GetSequenceDirectories(settings);

            if (directories == null || directories.Length == 0)
            {
                Debug.LogWarning("InflateDeflateCacheBuilder: no sequences were found for cache export.");
                return;
            }

            var exportedCount = 0;
            for (var i = 0; i < directories.Length; i++)
            {
                var sequencePath = directories[i];
                var sequenceName = Path.GetFileName(sequencePath);

                var loader = new SequenceLoader();
                var loadResult = loader
                    .Load(new SequenceLoadRequest
                    {
                        SequencePath = sequencePath,
                        SequenceName = sequenceName,
                        NearestCenterCount = 6
                    });

                if (!loadResult.Success)
                    throw new BuildFailedException($"InflateDeflateCacheBuilder: failed to load sequence '{sequenceName}': {loadResult.ErrorMessage}");

                var adapter = new TvmEditingMasterInflateDeflateAdapter();
                if (!adapter.ExportCacheToStreamingAssets(sequenceName, loadResult.Frames, out var exportError))
                    throw new BuildFailedException($"InflateDeflateCacheBuilder: failed to export cache for '{sequenceName}': {exportError}");

                exportedCount++;
                Debug.Log($"InflateDeflateCacheBuilder: exported cache for '{sequenceName}' ({exportedCount}/{directories.Length}).");
            }

            Debug.Log($"InflateDeflateCacheBuilder: exported inflate/deflate caches for {exportedCount} sequences.");
        }
    }
}
