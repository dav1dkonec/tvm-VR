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

/// <summary>
/// Manages the loading, playback, editing and saving of the sequence
/// </summary>
public class Sequence : MonoBehaviour, ICenterSelectionListener
{
    private SequenceLoader sequenceLoader;
    private SequenceSaver sequenceSaver;
    private SequencePlaybackController playbackController;
    private SequenceMeshPresenter meshPresenter;
    private SequenceCenterPresenter centerPresenter;
    private SequenceBusyStateController busyStateController;
    private EditingCore editingCore;
    private MeshCollider sequenceMeshCollider;
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

        editingCore = new EditingCore(
            null,
            new TvmEditingMasterInflateDeflateAdapter());

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

        busyStateController.Enter(leftHand, rightHand, waitCanvas);
        var loadResult = await sequenceLoader.LoadAsync(new SequenceLoadRequest
        {
            SequencePath = sequencePath,
            SequenceName = sequenceName,
            NearestCenterCount = methodSettings != null ? methodSettings.SurfaceNeighborCount : 6
        });
        busyStateController.Exit(leftHand, rightHand, waitCanvas);

        if (!loadResult.Success)
        {
            Debug.LogError(loadResult.ErrorMessage + " Path: " + sequencePath);
            if (pl) Play();
            return;
        }

        frames = loadResult.Frames;
        settings = loadResult.Settings;
        playbackUI.SetFPS(settings.framerate);

        currentFrame = 0;
        centerPresenter.SyncPositions(centerPool, frames[currentFrame].centers);
        centerPool.SetInteractionEnabled(methodSettings == null || methodSettings.CurrentMethod == MethodKind.BasicTranslate);
        RedrawMesh();
        loadedPath = sequencePath;
        loadedName = sequenceName;
        saveButton.interactable = true;

        if (pl) Play();
    }

    /// <summary>
    /// Saves the current sequence asynchronously
    /// </summary>
    public async void SavePressed()
    {
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
        RedrawMesh();

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

        centerPresenter.SyncPositions(centerPool, frames[currentFrame].centers);
        RedrawMesh();

        busyStateController.Exit(leftHand, rightHand, waitCanvas);
        if (pl) Play();
    }

    public async void CommitInflateDeflate(UnityEngine.Vector3 referencePoint)
    {
        if (frames == null || frames.Length == 0)
        {
            Debug.LogWarning("Sequence: No sequence is loaded.");
            return;
        }

        var pl = playing;
        Pause();
        busyStateController.Enter(leftHand, rightHand, waitCanvas);

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
                    ReferencePoint = new Point3Data(referencePoint.x, referencePoint.y, referencePoint.z),
                    Radius = methodSettings != null ? methodSettings.InflateRadius : 0.08f,
                    Strength = methodSettings != null ? methodSettings.InflateStrength : 0.02f,
                    Mode = methodSettings != null ? methodSettings.InflateMode : InflateDeflateMode.Inflate
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
        RedrawMesh();

        busyStateController.Exit(leftHand, rightHand, waitCanvas);
        if (pl) Play();
    }

    private SequenceRuntimeContext BuildRuntimeContext()
    {
        return new SequenceRuntimeContext
        {
            Frames = frames,
            SequenceId = loadedName ?? string.Empty,
            LoadedName = loadedName ?? string.Empty,
            CurrentFrameIndex = currentFrame,
            FrameCount = frames?.Length ?? 0,
            CenterCount = frames != null && frames.Length > 0 && frames[currentFrame] != null
                ? frames[currentFrame].centers.Length
                : 0,
            BasicTranslate = new BasicTranslateRuntimeConfiguration
            {
                CenterSigma = methodSettings != null ? methodSettings.CenterSigma : 1f,
                SequenceNeighborCount = methodSettings != null ? methodSettings.SequenceNeighborCount : 4,
                SurfaceNeighborCount = methodSettings != null ? methodSettings.SurfaceNeighborCount : 6
            },
            InflateDeflate = new InflateDeflateRuntimeConfiguration
            {
                Radius = methodSettings != null ? methodSettings.InflateRadius : 0.08f,
                Strength = methodSettings != null ? methodSettings.InflateStrength : 0.02f,
                Mode = methodSettings != null ? methodSettings.InflateMode : InflateDeflateMode.Inflate
            }
        };
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
