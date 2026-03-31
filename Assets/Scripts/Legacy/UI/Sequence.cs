using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CoreTvm;

/// <summary>
/// Manages the loading, playback, editing and saving of the sequence
/// </summary>
public class Sequence : MonoBehaviour, ICenterSelectionListener
{
    public EditingSession session;

    public EditingOptions editingOptions = new EditingOptions();
    public bool useUnifiedPrototypeOnCommit;
    public float editorPreviewDistance = 1.5f;
    public int offlineSequentialCommitCount = 3;

    [Header("Offline Comparison Test")]
    public int offlineTestCenterIndex;
    public UnityEngine.Vector3 offlineTestTranslation = new UnityEngine.Vector3(0.05f, 0f, 0f);
    public int[] offlineMultiCenterIndices = new int[0];
    public UnityEngine.Vector3[] offlineMultiCenterTranslations = new UnityEngine.Vector3[0];

    private EditingCoreAdapter coreAdapter;
    private SequenceEditingService editingService;
    private PipelineComparisonHarness comparisonHarness;
    private RuntimeTestRunner runtimeTestRunner;
    private bool isSequenceLoading;
    private bool pendingEditorCommit;
    private bool configuredUnifiedPrototype;
    private Frame[] loadedFramesSnapshot;
    private UnityEngine.Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;
    private UnityEngine.Vector3 initialLocalScale;

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
    /// The brush calls the deformation methods
    /// </summary>
    public Brush activeBrush;

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

    /// <summary>
    /// Initialization
    /// </summary>
    void Start()
    {
        CenterUI.RegisterListener(this);
        RebuildEditingPipeline();
        comparisonHarness = new PipelineComparisonHarness();
        runtimeTestRunner = new RuntimeTestRunner();
        initialLocalPosition = transform.localPosition;
        initialLocalRotation = transform.localRotation;
        initialLocalScale = transform.localScale;

        mesh = new Mesh();
        mesh.MarkDynamic();

        MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        playbackUI.SetFPS(24);
    }

    /// <summary>
    /// Updates the sequence to the current frame
    /// </summary>
    void Update()
    {
        if (!playing) return;

        if (time > playbackUI.GetSPF())
        {
            time -= playbackUI.GetSPF();
            Next();
        }

        time += Time.deltaTime;
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
        isSequenceLoading = true;
        EnsureEditingPipelineConfiguration();

        var centersPath = sequencePath + "/centers";
        var meshesPath = sequencePath + "/meshes";
        var settingsPath = sequencePath + "/settings.xml";

        bool centersExist = Directory.Exists(centersPath);
        bool meshesExist = Directory.Exists(meshesPath);
        bool settingsExist = File.Exists(settingsPath);
        var centers = Directory.GetFiles(centersPath);
        var meshes = Directory.GetFiles(meshesPath);

        var tryLoad = centersExist && meshesExist
            && (centers.Length == meshes.Length);

        if (tryLoad)
        {
            StartAsync();
            await Task.Run(() =>
            {
                var loadedCenters = CentersIO.LoadCentersFiles(centers);
                var loadedCenters2 = CentersIO.LoadCentersFiles(centers);

                frames = new Frame[centers.Length];
                for (int i = 0; i < frames.Length; i++)
                {
                    frames[i] = new();
                    frames[i].centers = loadedCenters[i];
                    frames[i].centersUnedited = loadedCenters2[i];
                    MeshIO.LoadMesh(meshes[i], out frames[i].vertices, out frames[i].faces);
                    MeshIO.LoadMesh(meshes[i], out frames[i].verticesUnedited, out frames[i].faces);
                    frames[i].FindNearest(ui.max);
                    Debug.Log(i);
                }
                return true;
            });
            StopAsync();

            if (settingsExist)
            {
                settings = File.Exists(settingsPath)
                ? Serialization.Deserialize<SequenceSettings>(settingsPath)
                : new();
                Serialization.Serialize(settings, settingsPath);

                playbackUI.SetFPS(settings.framerate);
            }

            currentFrame = 0;
            centerPool.Initialize(frames[currentFrame].centers.Length);
            centerPool.SetPositions(frames[currentFrame].centers);
            RedrawMesh();
            loadedFramesSnapshot = FrameSnapshot.Clone(frames);

            session = editingService.CreateSession(frames, settings, sequencePath, sequenceName, currentFrame, editingOptions);

            loadedPath = sequencePath;
            loadedName = sequenceName;
            saveButton.interactable = true;

            isSequenceLoading = false;

            if (pendingEditorCommit)
            {
                pendingEditorCommit = false;
                RunEditorCommitUsingOfflineTestParameters();
            }

        }
        else
        {
            isSequenceLoading = false;
            Debug.LogError("Failed to load sequence from path: " + sequencePath);
        }

        if (pl) Play();
    }

    /// <summary>
    /// Saves the current sequence asynchronously
    /// </summary>
    public async void SavePressed()
    {
        StartAsync();
        var text = waitCanvas.GetComponentInChildren<TMP_Text>();
        var old = text.text;

        if (loadedPath == null || !Directory.Exists(loadedPath))
            return;

        var dirname = loadedName + "_" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
        var path = loadedPath + "/" + dirname;

        var centersPath = path + "/centers";
        var meshesPath = path + "/meshes";
        var settingsPath = path + "/settings.xml";

        text.text = "saving to " + path.ToLower();

        Directory.CreateDirectory(path);
        Directory.CreateDirectory(centersPath);
        Directory.CreateDirectory(meshesPath);

        SequenceSettings settings = new();
        Serialization.Serialize(settings, settingsPath);

        await Task.Run(() =>
        {
            for (int i = 0; i < frames.Length; i++)
            {
                CentersIO.WriteXYZ(centersPath + $"/{i:0000}.xyz", frames[i].centers);
                MeshIO.WriteMesh(frames[i].vertices, frames[i].faces, meshesPath + $"/{i:0000}.obj");
            }
        });

        text.text = old;
        StopAsync();
    }

    /// <summary>
    /// Refreshes the mesh
    /// </summary>
    public void RedrawMesh()
    {
        mesh.Clear();
        mesh.vertices = frames[currentFrame].GetUnityVertices();
        mesh.triangles = frames[currentFrame].GetUnityFaces();
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }

    /// <summary>
    /// Receive selection notification
    /// </summary>
    /// <param name="center">Center</param>
    /// <param name="hovering">True on enter, false on exit</param>
    public void Notify(CenterUI center, bool selecting)
    {
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
        var pl = playing;
        Pause();
        StartAsync();
        EnsureEditingPipelineConfiguration();

        await Task.Run(() =>
        {
            if (useUnifiedPrototypeOnCommit)
            {
                return editingService.CommitUnifiedEdit(
                    session,
                    frames,
                    currentFrame,
                    center);
            }

            return editingService.CommitLegacyEdit(
                session,
                activeBrush,
                frames,
                settings,
                loadedPath,
                loadedName,
                currentFrame,
                center);
        });

        centerPool.SetPositions(frames[currentFrame].centers);
        RedrawMesh();

        StopAsync();
        if (pl) Play();
    }

    /// <summary>
    /// Starts up the editing pipeline asynchronously
    /// Commits all edits
    /// </summary>
    public async void CommitAllEdits()
    {
        var pl = playing;
        Pause();
        StartAsync();
        EnsureEditingPipelineConfiguration();

        await Task.Run(() =>
        {
            if (useUnifiedPrototypeOnCommit)
            {
                return editingService.CommitAllUnifiedEdits(
                    session,
                    frames);
            }

            return editingService.CommitAllLegacyEdits(
                session,
                activeBrush,
                frames,
                settings,
                loadedPath,
                loadedName,
                currentFrame);
        });

        centerPool.SetPositions(frames[currentFrame].centers);
        RedrawMesh();

        StopAsync();
        if (pl) Play();
    }

    [ContextMenu("Run Offline Prototype Comparison")]
    public void RunOfflinePrototypeComparison()
    {
        if (session?.Sequence == null)
        {
            Debug.LogError("Offline comparison failed: sequence is not loaded.");
            return;
        }

        if (frames == null || frames.Length == 0)
        {
            Debug.LogError("Offline comparison failed: legacy frames are not available.");
            return;
        }

        if (currentFrame < 0 || currentFrame >= frames.Length)
        {
            Debug.LogError("Offline comparison failed: current frame index is out of range.");
            return;
        }

        if (offlineTestCenterIndex < 0 || offlineTestCenterIndex >= frames[currentFrame].centers.Length)
        {
            Debug.LogError("Offline comparison failed: offlineTestCenterIndex is out of range.");
            return;
        }

        var preEditSequence = SequenceDataSnapshot.Clone(session.Sequence);
        var currentCenter = frames[currentFrame].centers[offlineTestCenterIndex];
        var targetPosition = new System.Numerics.Vector3(
            currentCenter.X + offlineTestTranslation.x,
            currentCenter.Y + offlineTestTranslation.y,
            currentCenter.Z + offlineTestTranslation.z);

        var request = new EditRequest
        {
            FrameIndex = currentFrame,
            CenterIndices = new[] { offlineTestCenterIndex },
            NewCenterPositions = new[] { targetPosition }
        };

        var legacyHarness = new LegacyPipelineHarness(activeBrush);
        var legacyResult = legacyHarness.RunSingleEdit(frames, settings, loadedPath, loadedName, request);
        var report = comparisonHarness.CompareSingleEdit(
            preEditSequence,
            legacyResult.Sequence,
            request,
            editingOptions,
            legacyResult.AffectedFrames);

        report.LegacyRuntimeMilliseconds = legacyResult.Profiling.TotalMilliseconds;
        report.LegacyCenterMilliseconds = legacyResult.Profiling.CenterMilliseconds;
        report.LegacyPropagationMilliseconds = legacyResult.Profiling.PropagationMilliseconds;
        report.LegacySurfaceMilliseconds = legacyResult.Profiling.SurfaceMilliseconds;

        Debug.Log(
            $"Offline comparison finished for sequence '{report.SequenceName}', frame {report.FrameIndex}, center {offlineTestCenterIndex}. " +
            $"Center delta mean/max: {report.MeanCenterDelta:F6}/{report.MaxCenterDelta:F6}. " +
            $"Vertex delta mean/max: {report.MeanVertexDelta:F6}/{report.MaxVertexDelta:F6}. " +
            $"Compared frames/centers/vertices: {report.ComparedFrameCount}/{report.ComparedCenterCount}/{report.ComparedVertexCount}. " +
            $"Legacy affected count/range: {report.LegacyAffectedFrameCount}/{report.LegacyAffectedFrameMin}-{report.LegacyAffectedFrameMax}. " +
            $"Unified affected count/range: {report.UnifiedAffectedFrameCount}/{report.UnifiedAffectedFrameMin}-{report.UnifiedAffectedFrameMax}. " +
            $"Legacy total/center/propagation/surface: {report.LegacyRuntimeMilliseconds:F3}/{report.LegacyCenterMilliseconds:F3}/{report.LegacyPropagationMilliseconds:F3}/{report.LegacySurfaceMilliseconds:F3} ms. " +
            $"Unified total/affinity/center/propagation/surface: {report.UnifiedRuntimeMilliseconds:F3}/{report.UnifiedAffinityMilliseconds:F3}/{report.UnifiedCenterMilliseconds:F3}/{report.UnifiedPropagationMilliseconds:F3}/{report.UnifiedSurfaceMilliseconds:F3} ms.");
    }

    [ContextMenu("Run Editor Commit Using Offline Test Parameters")]
    public void RunEditorCommitUsingOfflineTestParameters()
    {
        EnsureEditingPipelineConfiguration();

        if (isSequenceLoading)
        {
            pendingEditorCommit = true;
            Debug.Log("Editor commit queued until sequence loading finishes.");
            return;
        }

        if (session?.Sequence == null)
        {
            Debug.LogError("Editor commit failed: sequence is not loaded.");
            return;
        }

        if (frames == null || frames.Length == 0)
        {
            Debug.LogError("Editor commit failed: legacy frames are not available.");
            return;
        }

        if (currentFrame < 0 || currentFrame >= frames.Length)
        {
            Debug.LogError("Editor commit failed: current frame index is out of range.");
            return;
        }

        if (offlineTestCenterIndex < 0 || offlineTestCenterIndex >= frames[currentFrame].centers.Length)
        {
            Debug.LogError("Editor commit failed: offlineTestCenterIndex is out of range.");
            return;
        }

        var scenario = RuntimeTestScenario.CreateSingle(
            useUnifiedPrototypeOnCommit,
            currentFrame,
            offlineTestCenterIndex,
            offlineTestTranslation);
        var result = runtimeTestRunner.RunSingle(
            scenario,
            editingService,
            activeBrush,
            session,
            frames,
            settings,
            loadedPath,
            loadedName);

        if (!result.Success)
        {
            Debug.LogError(result.ErrorMessage);
            return;
        }

        centerPool.SetPositions(frames[currentFrame].centers);
        RedrawMesh();
        Debug.Log(result.ToLogMessage());
    }

    [ContextMenu("Run Sequential Editor Commits Using Offline Test Parameters")]
    public void RunSequentialEditorCommitsUsingOfflineTestParameters()
    {
        if (offlineSequentialCommitCount < 1)
        {
            Debug.LogError("Sequential editor commit failed: offlineSequentialCommitCount must be at least 1.");
            return;
        }

        Debug.Log(
            $"Starting {offlineSequentialCommitCount} sequential editor commits using {(useUnifiedPrototypeOnCommit ? "UNIFIED" : "LEGACY")} pipeline on sequence '{loadedName}'.");

        var scenario = RuntimeTestScenario.CreateSingle(
            useUnifiedPrototypeOnCommit,
            currentFrame,
            offlineTestCenterIndex,
            offlineTestTranslation);
        scenario.RepeatCount = offlineSequentialCommitCount;

        var results = runtimeTestRunner.RunSequential(
            scenario,
            editingService,
            activeBrush,
            session,
            frames,
            settings,
            loadedPath,
            loadedName);

        foreach (var result in results)
        {
            if (!result.Success)
            {
                Debug.LogError(result.ErrorMessage);
                return;
            }

            Debug.Log(result.ToLogMessage());
        }

        centerPool.SetPositions(frames[currentFrame].centers);
        RedrawMesh();
    }

    [ContextMenu("Run Multi-Center Editor Commit Using Offline Test Parameters")]
    public void RunMultiCenterEditorCommitUsingOfflineTestParameters()
    {
        EnsureEditingPipelineConfiguration();

        if (isSequenceLoading)
        {
            pendingEditorCommit = true;
            Debug.Log("Multi-center editor commit queued until sequence loading finishes.");
            return;
        }

        if (session?.Sequence == null)
        {
            Debug.LogError("Multi-center editor commit failed: sequence is not loaded.");
            return;
        }

        if (frames == null || frames.Length == 0)
        {
            Debug.LogError("Multi-center editor commit failed: legacy frames are not available.");
            return;
        }

        if (currentFrame < 0 || currentFrame >= frames.Length)
        {
            Debug.LogError("Multi-center editor commit failed: current frame index is out of range.");
            return;
        }

        if (offlineMultiCenterIndices == null || offlineMultiCenterTranslations == null || offlineMultiCenterIndices.Length == 0)
        {
            Debug.LogError("Multi-center editor commit failed: offlineMultiCenterIndices and offlineMultiCenterTranslations must be configured.");
            return;
        }

        if (offlineMultiCenterIndices.Length != offlineMultiCenterTranslations.Length)
        {
            Debug.LogError("Multi-center editor commit failed: offlineMultiCenterIndices and offlineMultiCenterTranslations must have the same length.");
            return;
        }

        for (var i = 0; i < offlineMultiCenterIndices.Length; i++)
        {
            var centerIndex = offlineMultiCenterIndices[i];
            if (centerIndex < 0 || centerIndex >= frames[currentFrame].centers.Length)
            {
                Debug.LogError($"Multi-center editor commit failed: center index {centerIndex} is out of range.");
                return;
            }
        }

        var scenario = RuntimeTestScenario.CreateMulti(
            useUnifiedPrototypeOnCommit,
            currentFrame,
            (int[])offlineMultiCenterIndices.Clone(),
            (UnityEngine.Vector3[])offlineMultiCenterTranslations.Clone());
        var result = runtimeTestRunner.RunSingle(
            scenario,
            editingService,
            activeBrush,
            session,
            frames,
            settings,
            loadedPath,
            loadedName);

        if (!result.Success)
        {
            Debug.LogError(result.ErrorMessage);
            return;
        }

        centerPool.SetPositions(frames[currentFrame].centers);
        RedrawMesh();
        Debug.Log(result.ToLogMessage());
    }

    [ContextMenu("Place Sequence In Front Of Main Camera")]
    public void PlaceSequenceInFrontOfMainCamera()
    {
        var targetCamera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
        if (targetCamera == null)
        {
            Debug.LogError("Sequence preview placement failed: no camera was found.");
            return;
        }

        transform.position = targetCamera.transform.position + targetCamera.transform.forward * editorPreviewDistance;
        transform.rotation = Quaternion.LookRotation(targetCamera.transform.forward, targetCamera.transform.up);

        Debug.Log($"Sequence moved in front of camera at distance {editorPreviewDistance:F2}.");
    }

    [ContextMenu("Reset Sequence Transform To Initial")]
    public void ResetSequenceTransformToInitial()
    {
        transform.localPosition = initialLocalPosition;
        transform.localRotation = initialLocalRotation;
        transform.localScale = initialLocalScale;

        Debug.Log("Sequence transform reset to initial values.");
    }

    [ContextMenu("Restore Loaded Sequence Snapshot")]
    public void RestoreLoadedSequenceSnapshot()
    {
        if (loadedFramesSnapshot == null || loadedFramesSnapshot.Length == 0)
        {
            Debug.LogError("Restore failed: no loaded sequence snapshot is available.");
            return;
        }

        EnsureEditingPipelineConfiguration();

        frames = FrameSnapshot.Clone(loadedFramesSnapshot);
        currentFrame = 0;
        session = editingService.CreateSession(frames, settings, loadedPath, loadedName, currentFrame, editingOptions);
        if (centerPool.centers == null || centerPool.centers.Length != frames[currentFrame].centers.Length)
            centerPool.Initialize(frames[currentFrame].centers.Length);
        centerPool.SetPositions(frames[currentFrame].centers);
        RedrawMesh();

        Debug.Log($"Sequence '{loadedName}' restored to the post-load snapshot.");
    }

    private void EnsureEditingPipelineConfiguration()
    {
        if (editingService == null || configuredUnifiedPrototype != useUnifiedPrototypeOnCommit)
            RebuildEditingPipeline();
    }

    private void ResetLegacyTimers()
    {
        activeBrush?.centerDeformation?.ResetTimer();
        activeBrush?.sequenceDeformation?.ResetTimer();
        activeBrush?.surfaceDeformation?.ResetTimer();
    }

    private static float ReadLegacyTimerMilliseconds(object target)
    {
        if (target == null)
            return 0f;

        var field = target.GetType().GetField("timer", BindingFlags.Instance | BindingFlags.Public);
        if (field == null)
            return 0f;

        var value = field.GetValue(target);
        if (value is long longValue)
            return longValue;
        if (value is int intValue)
            return intValue;

        return 0f;
    }

    private static List<int> ComputeAffectedFrames(Frame[] beforeFrames, Frame[] afterFrames)
    {
        var affected = new List<int>();
        if (beforeFrames == null || afterFrames == null)
            return affected;

        var frameCount = Math.Min(beforeFrames.Length, afterFrames.Length);
        for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
        {
            if (FrameChanged(beforeFrames[frameIndex], afterFrames[frameIndex]))
                affected.Add(frameIndex);
        }

        return affected;
    }

    private static bool FrameChanged(Frame beforeFrame, Frame afterFrame)
    {
        if (beforeFrame == null || afterFrame == null)
            return beforeFrame != afterFrame;

        if (AnyVectorChanged(beforeFrame.centers, afterFrame.centers))
            return true;

        if (AnyVectorChanged(beforeFrame.vertices, afterFrame.vertices))
            return true;

        return false;
    }

    private static bool AnyVectorChanged(System.Numerics.Vector3[] before, System.Numerics.Vector3[] after)
    {
        if (before == null || after == null)
            return before != after;

        var count = Math.Min(before.Length, after.Length);
        for (var i = 0; i < count; i++)
        {
            if (System.Numerics.Vector3.DistanceSquared(before[i], after[i]) > 1e-12f)
                return true;
        }

        return before.Length != after.Length;
    }

    private static (float mean, float max) ComputeLocalCenterMetrics(Frame beforeFrame, Frame afterFrame, int editedCenterIndex, int neighborCount)
    {
        return ComputeLocalCenterMetrics(beforeFrame, afterFrame, new[] { editedCenterIndex }, neighborCount);
    }

    private static (float mean, float max) ComputeLocalCenterMetrics(Frame beforeFrame, Frame afterFrame, int[] editedCenterIndices, int neighborCount)
    {
        if (beforeFrame?.centers == null || afterFrame?.centers == null || beforeFrame.centers.Length == 0 || editedCenterIndices == null || editedCenterIndices.Length == 0)
            return (0f, 0f);

        var editedSet = new HashSet<int>(editedCenterIndices);
        var neighborCandidates = new List<(int index, float distance)>(beforeFrame.centers.Length);

        foreach (var editedCenterIndex in editedCenterIndices)
        {
            var source = beforeFrame.centers[editedCenterIndex];
            for (var i = 0; i < beforeFrame.centers.Length; i++)
            {
                if (editedSet.Contains(i))
                    continue;

                neighborCandidates.Add((i, System.Numerics.Vector3.Distance(source, beforeFrame.centers[i])));
            }
        }

        neighborCandidates.Sort((left, right) => left.distance.CompareTo(right.distance));
        var uniqueNeighbors = new List<int>(neighborCount);
        for (var i = 0; i < neighborCandidates.Count && uniqueNeighbors.Count < neighborCount; i++)
        {
            if (!uniqueNeighbors.Contains(neighborCandidates[i].index))
                uniqueNeighbors.Add(neighborCandidates[i].index);
        }

        if (uniqueNeighbors.Count == 0)
            return (0f, 0f);

        var sum = 0f;
        var max = 0f;
        for (var i = 0; i < uniqueNeighbors.Count; i++)
        {
            var neighborIndex = uniqueNeighbors[i];
            var displacement = System.Numerics.Vector3.Distance(beforeFrame.centers[neighborIndex], afterFrame.centers[neighborIndex]);
            sum += displacement;
            if (displacement > max)
                max = displacement;
        }

        return (sum / uniqueNeighbors.Count, max);
    }

    private static (float mean, float max) ComputeLocalMeshMetrics(Frame beforeFrame, Frame afterFrame, int editedCenterIndex)
    {
        return ComputeLocalMeshMetrics(beforeFrame, afterFrame, new[] { editedCenterIndex });
    }

    private static (float mean, float max) ComputeLocalMeshMetrics(Frame beforeFrame, Frame afterFrame, int[] editedCenterIndices)
    {
        if (beforeFrame?.vertices == null || afterFrame?.vertices == null || beforeFrame.nearestCentersIndex == null || editedCenterIndices == null || editedCenterIndices.Length == 0)
            return (0f, 0f);

        var editedSet = new HashSet<int>(editedCenterIndices);
        var sum = 0f;
        var max = 0f;
        var count = 0;

        for (var vertexIndex = 0; vertexIndex < beforeFrame.vertices.Length; vertexIndex++)
        {
            var nearestCenters = beforeFrame.nearestCentersIndex[vertexIndex];
            if (!ContainsAnyCenterIndex(nearestCenters, editedSet))
                continue;

            var displacement = System.Numerics.Vector3.Distance(beforeFrame.vertices[vertexIndex], afterFrame.vertices[vertexIndex]);
            sum += displacement;
            if (displacement > max)
                max = displacement;
            count++;
        }

        return count > 0 ? (sum / count, max) : (0f, 0f);
    }

    private static bool ContainsAnyCenterIndex(int[] indices, HashSet<int> targetIndices)
    {
        if (indices == null || targetIndices == null || targetIndices.Count == 0)
            return false;

        for (var i = 0; i < indices.Length; i++)
        {
            if (targetIndices.Contains(indices[i]))
                return true;
        }

        return false;
    }

    private static string FormatCenterIndices(int[] centerIndices)
    {
        if (centerIndices == null || centerIndices.Length == 0)
            return "none";

        return string.Join(",", centerIndices);
    }

    private static string FormatAffectedFrameRange(List<int> affectedFrames)
    {
        if (affectedFrames == null || affectedFrames.Count == 0)
            return "none";

        return $"{affectedFrames[0]}-{affectedFrames[affectedFrames.Count - 1]}";
    }

    private void RebuildEditingPipeline()
    {
        configuredUnifiedPrototype = useUnifiedPrototypeOnCommit;
        coreAdapter = new EditingCoreAdapter(configuredUnifiedPrototype
            ? new PrototypeUnifiedEditingCore()
            : new PassthroughEditingCore());
        editingService = new SequenceEditingService(coreAdapter);
    }

    /// <summary>
    /// Maintenance before asynchronous method execution
    /// </summary>
    private void StartAsync()
    {
        leftHand.SetActive(false);
        rightHand.SetActive(false);
        waitCanvas.SetActive(true);
        editing = true;
    }

    /// <summary>
    /// Maintenance after asynchronous method execution
    /// </summary>
    private void StopAsync()
    {
        leftHand.SetActive(true);
        rightHand.SetActive(true);
        waitCanvas.SetActive(false);
        editing = false;
    }

    #region PLAYBACK CONTROLS

    /// <summary>
    /// Plays sequence. Looping.
    /// </summary>
    public void Play()
    {
        playing = true;
        time = 0;
    }

    /// <summary>
    /// Pauses sequence
    /// </summary>
    public void Pause()
    {
        playing = false;
    }

    /// <summary>
    /// Moves to the previous frame of the sequence. Looping.
    /// </summary>
    public void Previous()
    {
        currentFrame = (frames.Length + currentFrame - 1) % frames.Length;
        ToFrame(currentFrame);
    }

    /// <summary>
    /// Moves to the next frame of the sequence. Looping.
    /// </summary>
    public void Next()
    {
        currentFrame = (currentFrame + 1) % frames.Length;
        ToFrame(currentFrame);
    }

    /// <summary>
    /// Moves to first frame of the sequence.
    /// </summary>
    public void First()
    {
        currentFrame = 0;
        ToFrame(currentFrame);
    }

    /// <summary>
    /// Moves to last frame of the sequence.
    /// </summary>
    public void Last()
    {
        currentFrame = frames.Length - 1;
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
    
        editingService.UpdateCurrentFrame(session, currentFrame);
        playbackUI.progressSlider.value = ((float)currentFrame) / (frames.Length - 1);
        centerPool.SetPositions(frames[currentFrame].centers);
        RedrawMesh();
    }

    #endregion PLAYBACK CONTROLS

}
