using System;
using System.IO;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TvmVr2.Api.Common;
using TvmVr2.Api.Enums;
using TvmVr2.Api.Requests;
using TvmVr2.Api.Sequence;
using TvmVr2.Client.Centers;
using TvmVr2.Client.Mesh;
using TvmVr2.Client.Playback;
using TvmVr2.Client.Sequence;
using TvmVr2.Core;
using TvmVr2.Core.Methods.BasicTranslate;
using TvmVr2.Core.Methods.InflateDeflate;
using TvmVr2.Core.Methods.InflateDeflate.Profiling;
using Stopwatch = System.Diagnostics.Stopwatch;

/// <summary>
/// Manages the loading, playback, editing and saving of the sequence
/// </summary>
public class Sequence : MonoBehaviour, ICenterSelectionListener
{
    private TvmEditingMasterInflateDeflateAdapter inflateDeflateAdapter;
    private SequenceLoader sequenceLoader;
    private SequenceSaver sequenceSaver;
    private SequencePlaybackController playbackController;
    private SequenceMeshPresenter meshPresenter;
    private SequenceCenterPresenter centerPresenter;
    private SequenceBusyStateController busyStateController;
    private EditingCore editingCore;
    private MeshCollider sequenceMeshCollider;
    private InflateDeflateUI inflateDeflateUi;
    private SequenceData sequenceData;
    private RuntimeState runtimeState;
    public EditingMethodRuntimeSettings methodSettings;

    public SurfaceNeighborsUI ui;

    /// <summary>
    /// The playback controls
    /// </summary>
    public PlaybackUI playbackUI;
    
    /// <summary>
    /// The pool of centers
    /// </summary>
    public CenterPool centerPool;

    /// <summary>
    /// mesh used to display the sequence
    /// </summary>
    public Mesh mesh;

    /// <summary>
    /// Sequence settings
    /// </summary>
    public SequenceSettings settings;

    /// <summary>
    /// Sequence frame data
    /// </summary>
    public Frame[] frames;

    /// <summary>
    /// Current material of the sequence mesh
    /// </summary>
    public Material meshMaterial;

    /// <summary>
    /// Index of the current frame
    /// </summary>
    public int currentFrame;

    /// <summary>
    /// True if sequence is playing
    /// </summary>
    public static bool playing;
    /// <summary>
    /// True if sequence is being edited
    /// </summary>
    public static bool editing;

    /// <summary>
    /// Timer used to play the animation
    /// </summary>
    public float time;

    /// <summary>
    /// Left controller object
    /// </summary>
    public GameObject leftHand;

    /// <summary>
    /// Right controller object
    /// </summary>
    public GameObject rightHand;
    /// <summary>
    /// Canvas displaying the "processing" message
    /// </summary>
    public GameObject waitCanvas;

    /// <summary>
    /// Save button UI object
    /// </summary>
    public Button saveButton;

    /// <summary>
    /// Path to most recently loaded sequence
    /// </summary>
    public string loadedPath;

    /// <summary>
    /// Name of most recently loaded sequence
    /// </summary>
    public string loadedName;
    public bool HasPendingEdits { get; private set; }
    public event Action<bool> PendingEditsChanged;

    /// <summary>
    /// Initialization
    /// </summary>
    void Start()
    {
        CenterUI.RegisterListener(this);
        sequenceLoader = new SequenceLoader();
        sequenceSaver = new SequenceSaver();
        playbackController = new SequencePlaybackController();
        meshPresenter = new SequenceMeshPresenter();
        centerPresenter = new SequenceCenterPresenter();
        busyStateController = new SequenceBusyStateController();
        if (methodSettings == null)
        {
            methodSettings = FindFirstObjectByType<EditingMethodRuntimeSettings>();
            if (methodSettings == null)
            {
                var settingsObject = new GameObject("EditingMethodRuntimeSettings");
                methodSettings = settingsObject.AddComponent<EditingMethodRuntimeSettings>();
            }
        }

        inflateDeflateAdapter = new TvmEditingMasterInflateDeflateAdapter();
        editingCore = new EditingCore(
            null,
            inflateDeflateAdapter);

        mesh = new Mesh();
        mesh.MarkDynamic();

        MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            Debug.LogError("Sequence: MeshFilter is missing on Sequence object.");
            enabled = false;
            return;
        }

        sequenceMeshCollider = gameObject.GetComponent<MeshCollider>();
        if (sequenceMeshCollider == null)
            sequenceMeshCollider = gameObject.AddComponent<MeshCollider>();

        if (playbackUI == null)
        {
            Debug.LogError("Sequence: PlaybackUI is not assigned.");
            enabled = false;
            return;
        }

        if (centerPool == null)
        {
            Debug.LogError("Sequence: CenterPool is not assigned.");
            enabled = false;
            return;
        }

        if (waitCanvas == null)
        {
            Debug.LogError("Sequence: WaitCanvas is not assigned.");
            enabled = false;
            return;
        }

        if (saveButton == null)
        {
            Debug.LogError("Sequence: SaveButton is not assigned.");
            enabled = false;
            return;
        }

        meshFilter.mesh = mesh;

        playbackUI.SetFPS(24);
    }

    /// <summary>
    /// Updates the sequence to the current frame
    /// </summary>
    void Update()
    {
        if (!playing) return;

        if (playbackController.Tick(ref time, Time.deltaTime, playbackUI.GetSPF()))
        {
            Next();
        }
    }

    /// <summary>
    /// Loads a sequence from a given path
    /// </summary>
    /// <param name="sequencePath">Path to the sequence</param>
    /// <param name="sequenceName">Name of the sequence</param>
    public async void Load(string sequencePath, string sequenceName)
    {
        var pl = playing;
        Pause();

        var loadTimer = Stopwatch.StartNew();
        Debug.Log($"Sequence: starting load for '{sequenceName}' from '{sequencePath}'.");
        busyStateController.Enter(leftHand, rightHand, waitCanvas);
        var loadResult = await sequenceLoader.LoadAsync(new SequenceLoadRequest
        {
            SequencePath = sequencePath,
            SequenceName = sequenceName,
            // Match the original project behavior: precompute the full UI range,
            // not just the currently selected value.
            NearestCenterCount = ui != null ? ui.max : (methodSettings != null ? methodSettings.SurfaceNeighborCount : 6)
        });
        busyStateController.Exit(leftHand, rightHand, waitCanvas);

        if (!loadResult.Success)
        {
            Debug.LogError(loadResult.ErrorMessage + " Path: " + sequencePath);
            if (pl) Play();
            return;
        }
        loadTimer.Stop();

        sequenceData = loadResult.SequenceData ?? new SequenceData
        {
            SequenceId = sequenceName,
            Settings = loadResult.Settings,
            Topology = SequenceTopology.FromFrames(loadResult.Frames),
            OriginalFrames = loadResult.Frames
        };

        runtimeState = sequenceData.CreateRuntimeState();
        BindRuntimeState(runtimeState);
        playbackUI.SetFPS(settings.framerate);

        currentFrame = 0;
        centerPresenter.SyncPositions(centerPool, frames[currentFrame].centers);
        centerPool.SetInteractionEnabled(methodSettings == null || methodSettings.CurrentMethod != MethodKind.LoopSequence);
        RedrawMesh();
        loadedPath = sequencePath;
        loadedName = sequenceName;
        saveButton.interactable = true;
        SetPendingEdits(false);
        Debug.Log(
            $"Sequence: loaded '{sequenceName}' in {loadTimer.Elapsed.TotalMilliseconds:F2} ms " +
            $"(frames={frames?.Length ?? 0}, topologyFrames={sequenceData?.Topology?.FrameCount ?? 0}, runtimeRevisions={runtimeState?.FrameRevision?.Length ?? 0}).");
        if (pl) Play();
    }

    /// <summary>
    /// Saves the current sequence asynchronously
    /// </summary>
    public async void SavePressed()
    {
        if (InflateDeflateUI.BlockIfPickActive())
            return;

        if (loadedPath == null || !Directory.Exists(loadedPath))
        {
            Debug.LogWarning("Sequence: No loaded sequence directory is available for save.");
            return;
        }

        busyStateController.Enter(leftHand, rightHand, waitCanvas);
        var text = waitCanvas.GetComponentInChildren<TMP_Text>();
        var old = text.text;

        var timestamp = DateTime.Now;
        var outputPath = sequenceSaver.BuildOutputDirectoryPath(loadedPath, loadedName, timestamp);
        text.text = "saving to " + outputPath.ToLower();

        var saveResult = await sequenceSaver.SaveAsync(new SequenceSaveRequest
        {
            BaseDirectoryPath = loadedPath,
            SequenceName = loadedName,
            Frames = frames,
            Timestamp = timestamp
        });

        text.text = old;
        busyStateController.Exit(leftHand, rightHand, waitCanvas);

        if (!saveResult.Success)
            Debug.LogError(saveResult.ErrorMessage);
    }

    public void ReloadLoadedSequence()
    {
        if (InflateDeflateUI.BlockIfPickActive())
            return;

        if (sequenceData == null || sequenceData.OriginalFrames == null || sequenceData.OriginalFrames.Length == 0)
        {
            Debug.LogWarning("Sequence: No loaded sequence is available for reload.");
            return;
        }

        var pl = playing;
        Pause();
        var reloadTimer = Stopwatch.StartNew();

        if (runtimeState == null)
            runtimeState = sequenceData.CreateRuntimeState();
        runtimeState.ResetFrom(sequenceData);
        BindRuntimeState(runtimeState);

        var cacheResetTimer = Stopwatch.StartNew();
        var cacheHydrationState = "not_attempted";
        var cacheResetError = string.Empty;
        var cacheRehydrated = false;
        if (inflateDeflateAdapter != null)
        {
            cacheRehydrated = inflateDeflateAdapter.ResetExecutionContextToDefaultCache(
                loadedName ?? string.Empty,
                frames,
                out cacheHydrationState,
                out cacheResetError);
        }
        cacheResetTimer.Stop();
        if (!string.IsNullOrWhiteSpace(cacheResetError))
            Debug.LogWarning($"Sequence: inflate/deflate cache reload reported: {cacheResetError}");

        currentFrame = 0;
        centerPresenter.SyncPositions(centerPool, frames[currentFrame].centers);
        centerPool.SetInteractionEnabled(methodSettings == null || methodSettings.CurrentMethod != MethodKind.LoopSequence);
        RedrawMesh();
        saveButton.interactable = true;
        SetPendingEdits(false);
        reloadTimer.Stop();
        Debug.Log(
            $"Sequence: runtime state reloaded from SequenceData for '{loadedName ?? string.Empty}' in {reloadTimer.Elapsed.TotalMilliseconds:F2} ms " +
            $"(frames={frames?.Length ?? 0}, dirtyFramesCleared=true, cacheRehydrated={cacheRehydrated}, cacheHydrationState={cacheHydrationState}, cacheResetMs={cacheResetTimer.Elapsed.TotalMilliseconds:F2}).");

        if (pl) Play();
    }

    public void SetInflateDeflateStreamingCacheEnabled(bool enabled)
    {
        inflateDeflateAdapter?.SetStreamingAssetsCacheEnabled(enabled);
    }

    public async void ExportInflateDeflateCacheToStreamingAssets()
    {
        if (frames == null || frames.Length == 0)
        {
            Debug.LogWarning("Sequence: No loaded sequence is available for inflate/deflate cache export.");
            return;
        }

        if (string.IsNullOrWhiteSpace(loadedName))
        {
            Debug.LogWarning("Sequence: Loaded sequence name is missing for inflate/deflate cache export.");
            return;
        }

        if (inflateDeflateAdapter == null)
        {
            Debug.LogWarning("Sequence: Inflate/deflate adapter is not available for cache export.");
            return;
        }

        var pl = playing;
        Pause();
        busyStateController.Enter(leftHand, rightHand, waitCanvas);

        var waitText = waitCanvas != null ? waitCanvas.GetComponentInChildren<TMP_Text>() : null;
        var previousWaitText = waitText != null ? waitText.text : string.Empty;
        if (waitText != null)
            waitText.text = "exporting inflate/deflate cache...";

        var snapshot = FrameSnapshot.Clone(sequenceData?.OriginalFrames ?? frames);

        try
        {
            var execution = await Task.Run(() =>
            {
                var result = inflateDeflateAdapter.ExportCacheToStreamingAssets(loadedName, snapshot, out var localErrorMessage);
                return (result, localErrorMessage);
            });

            if (!execution.Item1)
            {
                Debug.LogError($"Sequence: Inflate/deflate cache export failed: {execution.Item2}");
                return;
            }

            Debug.Log($"Sequence: Inflate/deflate cache exported to {Path.Combine(UnityEngine.Application.streamingAssetsPath, "InflateDeflateCache", loadedName)}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Sequence: Inflate/deflate cache export failed with exception: {ex}");
        }
        finally
        {
            if (waitText != null)
                waitText.text = previousWaitText;

            busyStateController.Exit(leftHand, rightHand, waitCanvas);
            if (pl) Play();
        }
    }

    private void SetPendingEdits(bool value)
    {
        if (HasPendingEdits == value)
            return;

        HasPendingEdits = value;
        PendingEditsChanged?.Invoke(value);
    }

    /// <summary>
    /// Refreshes the mesh
    /// </summary>
    public void RedrawMesh()
    {
        meshPresenter.Redraw(mesh, frames[currentFrame]);

        if (sequenceMeshCollider != null)
        {
            sequenceMeshCollider.sharedMesh = null;
            sequenceMeshCollider.sharedMesh = mesh;
        }
    }

    /// <summary>
    /// Receive selection notification
    /// </summary>
    /// <param name="center">Center</param>
    /// <param name="hovering">True on enter, false on exit</param>
    public void Notify(CenterUI center, bool selecting)
    {
        var methodKind = methodSettings != null ? methodSettings.CurrentMethod : MethodKind.BasicTranslate;

        if (methodKind == MethodKind.InflateDeflate)
        {
            if (!selecting)
                return;

            if (inflateDeflateUi == null)
                inflateDeflateUi = FindFirstObjectByType<InflateDeflateUI>();

            inflateDeflateUi?.SelectReferenceCenter(center);
            return;
        }

        if (!selecting)
        {
            CommitEdit(center);
        }
    }

    /// <summary>
    /// Starts up the editing pipeline asynchronously
    /// </summary>
    /// <param name="center">The edited center</param>
    public async void CommitEdit(CenterUI center)
    {
        if (frames == null || frames.Length == 0)
        {
            Debug.LogWarning("Sequence: No sequence is loaded.");
            return;
        }

        var pl = playing;
        Pause();
        busyStateController.Enter(leftHand, rightHand, waitCanvas);
        var centerIndex = center.centerIndex;
        var position = center.transform.localPosition;

        var succeeded = await Task.Run(() =>
        {
            if (editingCore == null)
                return false;

            var methodKind = methodSettings != null ? methodSettings.CurrentMethod : MethodKind.BasicTranslate;
            if (methodKind != MethodKind.BasicTranslate)
                return false;

            var result = editingCore.Execute(
                new BasicTranslateRequest
                {
                    SequenceId = loadedName ?? string.Empty,
                    FrameIndex = currentFrame,
                    CenterIndex = centerIndex,
                    TargetPosition = new Point3Data(position.x, position.y, position.z)
                },
                BuildRuntimeContext());

            if (!result.Succeeded)
            {
                Debug.LogError($"Sequence: {result.ErrorMessage}");
            }

            return result.Succeeded;
        });

        if (!succeeded)
        {
            busyStateController.Exit(leftHand, rightHand, waitCanvas);
            if (pl) Play();
            return;
        }

        centerPresenter.SyncPositions(centerPool, frames[currentFrame].centers);
        SetPendingEdits(true);

        busyStateController.Exit(leftHand, rightHand, waitCanvas);
        if (pl) Play();
    }

    /// <summary>
    /// Starts up the editing pipeline asynchronously
    /// Commits all edits
    /// </summary>
    public async void CommitAllEdits()
    {
        if (frames == null || frames.Length == 0)
        {
            Debug.LogWarning("Sequence: No sequence is loaded.");
            return;
        }

        var pl = playing;
        var beforeVertices = CloneVertices(frames[currentFrame]?.vertices);
        Pause();
        busyStateController.Enter(leftHand, rightHand, waitCanvas);

        var succeeded = await Task.Run(() =>
        {
            if (editingCore == null)
                return false;

            var methodKind = methodSettings != null ? methodSettings.CurrentMethod : MethodKind.BasicTranslate;
            if (methodKind != MethodKind.BasicTranslate)
                return false;

            return editingCore.RebuildBasicTranslateSurface(
                frames,
                methodSettings != null ? methodSettings.SurfaceNeighborCount : 6);
        });

        if (!succeeded)
        {
            Debug.LogError("Sequence: BasicTranslate surface rebuild failed.");
            busyStateController.Exit(leftHand, rightHand, waitCanvas);
            if (pl) Play();
            return;
        }

        LogSurfaceDelta(beforeVertices, frames[currentFrame]?.vertices);

        centerPresenter.SyncPositions(centerPool, frames[currentFrame].centers);
        RedrawMesh();

        busyStateController.Exit(leftHand, rightHand, waitCanvas);
        if (pl) Play();
    }

    private static System.Numerics.Vector3[] CloneVertices(System.Numerics.Vector3[] source)
    {
        if (source == null)
            return null;

        var clone = new System.Numerics.Vector3[source.Length];
        Array.Copy(source, clone, source.Length);
        return clone;
    }

    private static void LogSurfaceDelta(System.Numerics.Vector3[] before, System.Numerics.Vector3[] after)
    {
        if (before == null || after == null)
        {
            Debug.Log("Sequence: Deform diagnostics skipped because vertex data are missing.");
            return;
        }

        if (before.Length != after.Length)
        {
            Debug.Log($"Sequence: Deform changed vertex count from {before.Length} to {after.Length}.");
            return;
        }

        float maxDistance = 0f;
        double totalDistance = 0d;
        int changedCount = 0;

        for (var i = 0; i < before.Length; i++)
        {
            var distance = System.Numerics.Vector3.Distance(before[i], after[i]);
            if (distance > 1e-6f)
                changedCount++;

            if (distance > maxDistance)
                maxDistance = distance;

            totalDistance += distance;
        }

        var averageDistance = before.Length > 0 ? totalDistance / before.Length : 0d;
        Debug.Log(
            $"Sequence: Deform diagnostics | changedVertices={changedCount}/{before.Length}, " +
            $"maxShift={maxDistance:F6}, avgShift={averageDistance:F6}");
    }

    private static int[] CollectChangedFrames(Frame[] beforeFrames, Frame[] afterFrames)
    {
        if (beforeFrames == null || afterFrames == null)
            return Array.Empty<int>();

        var changedFrames = new System.Collections.Generic.List<int>();
        var frameCount = Math.Min(beforeFrames.Length, afterFrames.Length);
        for (var i = 0; i < frameCount; i++)
        {
            if (FrameChanged(beforeFrames[i], afterFrames[i]))
                changedFrames.Add(i);
        }

        return changedFrames.ToArray();
    }

    private static bool FrameChanged(Frame before, Frame after)
    {
        if (before == null || after == null)
            return before != after;

        if (!VectorArraysEqual(before.centers, after.centers))
            return true;

        if (!VectorArraysEqual(before.vertices, after.vertices))
            return true;

        return false;
    }

    private static bool VectorArraysEqual(System.Numerics.Vector3[] left, System.Numerics.Vector3[] right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left == null || right == null || left.Length != right.Length)
            return false;

        for (var i = 0; i < left.Length; i++)
        {
            if (System.Numerics.Vector3.Distance(left[i], right[i]) > 1e-6f)
                return false;
        }

        return true;
    }

    public async void CommitInflateDeflate(int selectedCenterIndex)
    {
        if (frames == null || frames.Length == 0)
        {
            Debug.LogWarning("Sequence: No sequence is loaded.");
            return;
        }

        if (selectedCenterIndex < 0 || selectedCenterIndex >= frames[currentFrame].centers.Length)
        {
            Debug.LogWarning($"Sequence: InflateDeflate selected center index {selectedCenterIndex} is out of range.");
            return;
        }

        var pl = playing;
        var beforeFrames = FrameSnapshot.Clone(frames);
        Pause();
        busyStateController.Enter(leftHand, rightHand, waitCanvas);
        var commitTimer = Stopwatch.StartNew();

        try
        {
            var succeeded = await Task.Run(() =>
            {
                if (editingCore == null)
                    return false;

                var methodKind = methodSettings != null ? methodSettings.CurrentMethod : MethodKind.BasicTranslate;
                if (methodKind != MethodKind.InflateDeflate)
                    return false;

                var result = editingCore.Execute(
                    new InflateDeflateRequest
                    {
                        SequenceId = loadedName ?? string.Empty,
                        FrameIndex = currentFrame,
                        SelectedCenterIndex = selectedCenterIndex,
                        Radius = methodSettings != null ? methodSettings.InflateRadius : 0.08f,
                        Strength = methodSettings != null ? methodSettings.InflateStrength : 0.02f,
                        Mode = methodSettings != null ? methodSettings.InflateMode : InflateDeflateMode.Inflate
                    },
                    BuildRuntimeContext());

                if (!result.Succeeded)
                    Debug.LogError($"Sequence: {result.ErrorMessage}");

                return result.Succeeded;
            });

            if (!succeeded)
                return;

            commitTimer.Stop();
            centerPresenter.SyncPositions(centerPool, frames[currentFrame].centers);
            RedrawMesh();
            var changedFrames = CollectChangedFrames(beforeFrames, frames);
            LogInflateDeflateFrameChangeDiagnostics("commit", currentFrame, changedFrames, frames?.Length ?? 0);
            SetPendingEdits(true);
            Debug.Log(
                $"Sequence: inflate/deflate commit completed in {commitTimer.Elapsed.TotalMilliseconds:F2} ms " +
                $"(frame={currentFrame}, changedFrames={changedFrames.Length}, cacheEnabled={inflateDeflateAdapter?.IsStreamingAssetsCacheEnabled ?? false}, cacheSource=OriginalFrames).");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Sequence: InflateDeflate execution failed with exception: {ex}");
        }
        finally
        {
            busyStateController.Exit(leftHand, rightHand, waitCanvas);
            if (pl) Play();
        }
    }

    public async void ApplyInflateDeflateDebug(
        int frameIndex,
        int centerIndex,
        float radius,
        float strength,
        InflateDeflateMode mode)
    {
        if (frames == null || frames.Length == 0)
        {
            Debug.LogWarning("Sequence: No sequence is loaded.");
            return;
        }

        if (frameIndex < 0 || frameIndex >= frames.Length || frames[frameIndex] == null)
        {
            Debug.LogWarning($"Sequence: Debug apply frame index {frameIndex} is out of range.");
            return;
        }

        if (frames[frameIndex].centers == null || centerIndex < 0 || centerIndex >= frames[frameIndex].centers.Length)
        {
            Debug.LogWarning($"Sequence: Debug apply center index {centerIndex} is out of range.");
            return;
        }

        if (radius <= 0f || strength <= 0f)
        {
            Debug.LogWarning("Sequence: Debug apply requires positive radius and strength.");
            return;
        }

        var pl = playing;
        var beforeFrames = FrameSnapshot.Clone(frames);
        Pause();
        busyStateController.Enter(leftHand, rightHand, waitCanvas);
        var debugApplyTimer = Stopwatch.StartNew();

        var waitText = waitCanvas != null ? waitCanvas.GetComponentInChildren<TMP_Text>() : null;
        var previousWaitText = waitText != null ? waitText.text : string.Empty;
        if (waitText != null)
            waitText.text = "applying inflate/deflate debug edit...";

        try
        {
            Debug.Log(
                $"InflateDeflateDebugApply: frame={frameIndex}, center={centerIndex}, " +
                $"radius={radius:F2}, strength={strength:F2}, mode={mode}.");

            var succeeded = await Task.Run(() =>
            {
                if (editingCore == null)
                    return false;

                var result = editingCore.Execute(
                    new InflateDeflateRequest
                    {
                        SequenceId = loadedName ?? string.Empty,
                        FrameIndex = frameIndex,
                        SelectedCenterIndex = centerIndex,
                        Radius = radius,
                        Strength = strength,
                        Mode = mode
                    },
                    BuildRuntimeContext(frames, frameIndex, radius, strength, mode));

                if (!result.Succeeded)
                    Debug.LogError($"Sequence: {result.ErrorMessage}");

                return result.Succeeded;
            });

            if (!succeeded)
                return;

            debugApplyTimer.Stop();
            currentFrame = frameIndex;
            centerPresenter.SyncPositions(centerPool, frames[currentFrame].centers);
            RedrawMesh();
            var changedFrames = CollectChangedFrames(beforeFrames, frames);
            LogInflateDeflateFrameChangeDiagnostics("debug apply", frameIndex, changedFrames, frames?.Length ?? 0);
            SetPendingEdits(true);
            Debug.Log(
                $"Sequence: inflate/deflate debug apply completed in {debugApplyTimer.Elapsed.TotalMilliseconds:F2} ms " +
                $"(frame={frameIndex}, changedFrames={changedFrames.Length}, cacheEnabled={inflateDeflateAdapter?.IsStreamingAssetsCacheEnabled ?? false}, cacheSource=OriginalFrames).");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Sequence: InflateDeflate debug apply failed with exception: {ex}");
        }
        finally
        {
            if (waitText != null)
                waitText.text = previousWaitText;

            busyStateController.Exit(leftHand, rightHand, waitCanvas);
            if (pl) Play();
        }
    }

    public async void RunInflateDeflateQuickProfile(
        int frameIndex,
        int centerIndex,
        float radius,
        float strength,
        InflateDeflateMode mode,
        int iterations = 1,
        bool restoreOriginalStateAfterRun = true)
    {
        if (frames == null || frames.Length == 0)
        {
            Debug.LogWarning("Sequence: No sequence is loaded.");
            return;
        }

        if (frameIndex < 0 || frameIndex >= frames.Length || frames[frameIndex] == null)
        {
            Debug.LogWarning($"Sequence: Quick profile frame index {frameIndex} is out of range.");
            return;
        }

        if (frames[frameIndex].centers == null || centerIndex < 0 || centerIndex >= frames[frameIndex].centers.Length)
        {
            Debug.LogWarning($"Sequence: Quick profile center index {centerIndex} is out of range.");
            return;
        }

        if (radius <= 0f || strength <= 0f)
        {
            Debug.LogWarning("Sequence: Quick profile requires positive radius and strength.");
            return;
        }

        iterations = Mathf.Max(1, iterations);

        var pl = playing;
        var originalCurrentFrame = currentFrame;
        Pause();
        busyStateController.Enter(leftHand, rightHand, waitCanvas);

        var waitText = waitCanvas != null ? waitCanvas.GetComponentInChildren<TMP_Text>() : null;
        var previousWaitText = waitText != null ? waitText.text : string.Empty;
        if (waitText != null)
            waitText.text = "profiling inflate/deflate...";

        var originalFrames = FrameSnapshot.Clone(frames);
        var profiles = new System.Collections.Generic.List<InflateDeflateQuickProfile>(iterations);
        var adapter = inflateDeflateAdapter ?? new TvmEditingMasterInflateDeflateAdapter();

        try
        {
            for (var iteration = 0; iteration < iterations; iteration++)
            {
                var iterationFrames = FrameSnapshot.Clone(originalFrames);
                Debug.Log($"InflateDeflateQuickProfiler: starting iteration {iteration + 1}/{iterations}.");
                var request = new InflateDeflateRequest
                {
                    SequenceId = loadedName ?? string.Empty,
                    FrameIndex = frameIndex,
                    SelectedCenterIndex = centerIndex,
                    Radius = radius,
                    Strength = strength,
                    Mode = mode
                };

                var runtimeContext = BuildRuntimeContext(iterationFrames, frameIndex, radius, strength, mode);
                Debug.Log("InflateDeflateQuickProfiler: running test in phase ResolveEffectors.");
                var mapTimer = Stopwatch.StartNew();
                var input = (InflateDeflateMethodInput)editingCore.MapRequest(request, runtimeContext);
                mapTimer.Stop();

                Debug.Log("InflateDeflateQuickProfiler: running test in phase AdapterExecute.");
                var execution = await Task.Run(() =>
                {
                    var result = adapter.ExecuteProfiled(input, out var profile);
                    return (result, profile);
                });

                var profile = execution.profile ?? new InflateDeflateQuickProfile();
                profile.Iteration = iteration + 1;
                profile.FrameIndex = frameIndex;
                profile.CenterIndex = centerIndex;
                profile.ResolveEffectorsMs = mapTimer.Elapsed.TotalMilliseconds;
                profile.CoreTotalMs = profile.ResolveEffectorsMs + profile.AdapterTotalMs;
                profile.Success = execution.result != null && execution.result.Success;
                profile.ErrorMessage = execution.result?.ErrorMessage ?? profile.ErrorMessage;

                if (!profile.Success)
                {
                    profiles.Add(profile);
                    Debug.LogError(profile.ToLogString("InflateDeflate Quick Profile Failed"));
                    if (restoreOriginalStateAfterRun)
                    {
                        frames = originalFrames;
                        currentFrame = frameIndex;
                        centerPresenter.SyncPositions(centerPool, frames[currentFrame].centers);
                        RedrawMesh();
                    }
                    return;
                }

                Debug.Log("InflateDeflateQuickProfiler: running test in phase UnityApply.");
                var unityApplyTimer = Stopwatch.StartNew();
                frames = iterationFrames;
                currentFrame = frameIndex;
                centerPresenter.SyncPositions(centerPool, frames[currentFrame].centers);
                RedrawMesh();
                unityApplyTimer.Stop();

                profile.UnityApplyMs = unityApplyTimer.Elapsed.TotalMilliseconds;
                profile.TotalMs = profile.CoreTotalMs + profile.UnityApplyMs;
                profiles.Add(profile);

                Debug.Log(profile.ToLogString("InflateDeflate Quick Profile"));
                Debug.Log($"InflateDeflateQuickProfiler: iteration {iteration + 1}/{iterations} completed.");
            }

            var average = InflateDeflateQuickProfile.Average(profiles);
            Debug.Log("InflateDeflateQuickProfiler: all iterations completed. Printing average profile.");
            Debug.Log(average.ToLogString("InflateDeflate Quick Profile Average"));
        }
        catch (Exception ex)
        {
            Debug.LogError($"Sequence: InflateDeflate quick profile failed with exception: {ex}");
        }
        finally
        {
            if (restoreOriginalStateAfterRun)
            {
                frames = originalFrames;
                currentFrame = Mathf.Clamp(originalCurrentFrame, 0, frames.Length - 1);
                centerPresenter.SyncPositions(centerPool, frames[currentFrame].centers);
                RedrawMesh();
            }

            if (waitText != null)
                waitText.text = previousWaitText;

            busyStateController.Exit(leftHand, rightHand, waitCanvas);
            if (pl) Play();
        }
    }

    private SequenceRuntimeContext BuildRuntimeContext()
    {
        return BuildRuntimeContext(
            frames,
            currentFrame,
            methodSettings != null ? methodSettings.InflateRadius : 0.08f,
            methodSettings != null ? methodSettings.InflateStrength : 0.02f,
            methodSettings != null ? methodSettings.InflateMode : InflateDeflateMode.Inflate);
    }

    private SequenceRuntimeContext BuildRuntimeContext(
        Frame[] runtimeFrames,
        int runtimeFrameIndex,
        float inflateRadius,
        float inflateStrength,
        InflateDeflateMode inflateMode)
    {
        return new SequenceRuntimeContext
        {
            Frames = runtimeFrames,
            CacheFrames = sequenceData?.OriginalFrames ?? runtimeFrames,
            SequenceId = loadedName ?? string.Empty,
            LoadedName = loadedName ?? string.Empty,
            CurrentFrameIndex = runtimeFrameIndex,
            FrameCount = runtimeFrames?.Length ?? 0,
            CenterCount = runtimeFrames != null && runtimeFrames.Length > 0 && runtimeFrames[runtimeFrameIndex] != null
                ? runtimeFrames[runtimeFrameIndex].centers.Length
                : 0,
            BasicTranslate = new BasicTranslateRuntimeConfiguration
            {
                CenterSigma = methodSettings != null ? methodSettings.CenterSigma : 1f,
                SequenceNeighborCount = methodSettings != null ? methodSettings.SequenceNeighborCount : 4,
                SurfaceNeighborCount = methodSettings != null ? methodSettings.SurfaceNeighborCount : 6
            },
            InflateDeflate = new InflateDeflateRuntimeConfiguration
            {
                Radius = inflateRadius,
                Strength = inflateStrength,
                Mode = inflateMode
            }
        };
    }

    private void BindRuntimeState(RuntimeState state)
    {
        runtimeState = state;
        frames = runtimeState?.Frames ?? Array.Empty<Frame>();
        settings = sequenceData?.Settings ?? new SequenceSettings();
    }

    private static void LogInflateDeflateFrameChangeDiagnostics(string operation, int frameIndex, int[] changedFrames, int totalFrameCount)
    {
        var changedCount = changedFrames != null ? changedFrames.Length : 0;
        var fullInvalidation = totalFrameCount > 0 && changedCount == totalFrameCount;

        Debug.Log(
            $"Sequence: inflate/deflate {operation} frame change diagnostics | " +
            $"editedFrame={frameIndex}, changedFrames={changedCount}/{totalFrameCount}, " +
            $"fullInvalidation={fullInvalidation}, changedFrameIndices=[{string.Join(", ", changedFrames ?? Array.Empty<int>())}].");

        if (fullInvalidation)
        {
            Debug.LogWarning(
                $"Sequence: inflate/deflate {operation} touched the entire runtime state. " +
                "This is only expected when the deformation propagates to every frame.");
        }
    }

    #region PLAYBACK CONTROLS

    /// <summary>
    /// Plays sequence. Looping.
    /// </summary>
    public void Play()
    {
        playbackController.Play(ref playing, ref time);
    }

    /// <summary>
    /// Pauses sequence
    /// </summary>
    public void Pause()
    {
        playbackController.Pause(ref playing);
    }

    /// <summary>
    /// Moves to the previous frame of the sequence. Looping.
    /// </summary>
    public void Previous()
    {
        if (frames == null || frames.Length == 0)
        {
            Debug.LogWarning("Sequence: No sequence is loaded.");
            return;
        }

        currentFrame = playbackController.PreviousFrame(currentFrame, frames.Length);
        ToFrame(currentFrame);
    }

    /// <summary>
    /// Moves to the next frame of the sequence. Looping.
    /// </summary>
    public void Next()
    {
        if (frames == null || frames.Length == 0)
        {
            Debug.LogWarning("Sequence: No sequence is loaded.");
            return;
        }

        currentFrame = playbackController.NextFrame(currentFrame, frames.Length);
        ToFrame(currentFrame);
    }

    /// <summary>
    /// Moves to first frame of the sequence.
    /// </summary>
    public void First()
    {
        if (frames == null || frames.Length == 0)
        {
            Debug.LogWarning("Sequence: No sequence is loaded.");
            return;
        }

        currentFrame = playbackController.FirstFrame();
        ToFrame(currentFrame);
    }

    /// <summary>
    /// Moves to last frame of the sequence.
    /// </summary>
    public void Last()
    {
        if (frames == null || frames.Length == 0)
        {
            Debug.LogWarning("Sequence: No sequence is loaded.");
            return;
        }

        currentFrame = playbackController.LastFrame(frames.Length);
        ToFrame(currentFrame);
    }

    /// <summary>
    /// Moves to a given frame.
    /// </summary>
    /// <param name="currentFrame">Frame to move to.</param>
    private void ToFrame(int currentFrame)
    {
        if (frames == null || frames[currentFrame] == null || currentFrame < 0 ||centerPool == null)
            return;

        playbackUI.progressSlider.value = ((float)currentFrame) / (frames.Length - 1);
        centerPresenter.SyncPositions(centerPool, frames[currentFrame].centers);
        RedrawMesh();
    }

    #endregion PLAYBACK CONTROLS

}
