using System;
using System.IO;
using System.Diagnostics;
using TvmVr2.Client.Sequence;
using TvmVr2.Core.Methods.InflateDeflate;
using UnityEditor;
using UnityEngine;

namespace TvmVr2.Core.Methods.InflateDeflate.Cache.Editor
{
    public sealed class InflateDeflateCacheBuilder
    {
        [MenuItem("Tools/TVM VR/Build InflateDeflate Cache")]
        public static void BuildCache()
        {
            var totalTimer = Stopwatch.StartNew();
            var catalog = new SequenceCatalog();
            var settings = catalog.LoadOrCreateSettings("/settings.xml");
            var directories = catalog.GetSequenceDirectories(settings);

            if (directories == null || directories.Length == 0)
            {
                UnityEngine.Debug.LogWarning("InflateDeflateCacheBuilder: no sequences were found for cache export.");
                return;
            }

            var exportedCount = 0;
            UnityEngine.Debug.Log($"InflateDeflateCacheBuilder: starting cache build for {directories.Length} sequences.");
            for (var i = 0; i < directories.Length; i++)
            {
                var sequencePath = directories[i];
                var sequenceName = Path.GetFileName(sequencePath);

                var loader = new SequenceLoader();
                var loadTimer = Stopwatch.StartNew();
                var loadResult = loader.Load(new SequenceLoadRequest
                {
                    SequencePath = sequencePath,
                    SequenceName = sequenceName,
                    NearestCenterCount = 6
                });
                loadTimer.Stop();

                if (!loadResult.Success)
                    throw new Exception($"InflateDeflateCacheBuilder: failed to load sequence '{sequenceName}': {loadResult.ErrorMessage}");

                UnityEngine.Debug.Log(
                    $"InflateDeflateCacheBuilder: loaded '{sequenceName}' in {loadTimer.Elapsed.TotalMilliseconds:F2} ms " +
                    $"(frames={loadResult.Frames?.Length ?? 0}, cacheSeed={loadResult.SequenceData?.FrameCount ?? 0}).");

                var adapter = new TvmEditingMasterInflateDeflateAdapter();
                var exportTimer = Stopwatch.StartNew();
                if (!adapter.ExportCacheToStreamingAssets(sequenceName, loadResult.Frames, out var exportError))
                    throw new Exception($"InflateDeflateCacheBuilder: failed to export cache for '{sequenceName}': {exportError}");
                exportTimer.Stop();

                exportedCount++;
                UnityEngine.Debug.Log(
                    $"InflateDeflateCacheBuilder: exported cache for '{sequenceName}' " +
                    $"in {exportTimer.Elapsed.TotalMilliseconds:F2} ms ({exportedCount}/{directories.Length}).");
            }

            totalTimer.Stop();
            UnityEngine.Debug.Log(
                $"InflateDeflateCacheBuilder: exported inflate/deflate caches for {exportedCount} sequences " +
                $"in {totalTimer.Elapsed.TotalMilliseconds:F2} ms.");
        }
    }
}
