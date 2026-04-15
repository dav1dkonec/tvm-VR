using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TvmVr2.Client.Sequence;

/// <summary>
/// Initializes the application
/// </summary>
public class Controller : MonoBehaviour
{
    private SequenceCatalog sequenceCatalog;

    /// <summary>
    /// Path on which to look for the settings
    /// </summary>
    public string settingsPath = "/settings.xml";
    
    /// <summary>
    /// Application settings
    /// </summary>
    public Settings settings;

    /// <summary>
    /// Sequence canvas
    /// </summary>
    public SequenceUI sequenceUI;

    [Header("Editor Load Helper")]
    public int editorSequenceIndex;

    /// <summary>
    /// Loads the list of available sequences at start
    /// </summary>
    void Start()
    {
        System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
        sequenceCatalog = new SequenceCatalog();

        EnsureSettingsLoaded();

        foreach (var directory in sequenceCatalog.GetSequenceDirectories(settings))
        {
            var name = Path.GetFileName(directory);
            sequenceUI.Add(name, directory);
            Debug.Log("Sequence available: " + name);
        }
    }

    [ContextMenu("Load First Available Sequence")]
    public void LoadFirstAvailableSequence()
    {
        LoadSequenceByIndex(0);
    }

    [ContextMenu("Load Sequence By Editor Index")]
    public void LoadSequenceByEditorIndex()
    {
        LoadSequenceByIndex(editorSequenceIndex);
    }

    private void LoadSequenceByIndex(int index)
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("Sequence loading helper can be used only in Play Mode.");
            return;
        }

        if (sequenceUI == null || sequenceUI.sequence == null)
        {
            Debug.LogError("Sequence loading helper failed: SequenceUI or Sequence reference is missing.");
            return;
        }

        EnsureSettingsLoaded();

        if (settings == null || string.IsNullOrWhiteSpace(settings.dataPath))
        {
            Debug.LogError("Sequence loading helper failed: settings or data path is not initialized.");
            return;
        }

        var directories = sequenceCatalog.GetSequenceDirectories(settings);
        if (directories.Length == 0)
        {
            Debug.LogError("Sequence loading helper failed: no sequences found in " + settings.dataPath);
            return;
        }

        if (!sequenceCatalog.TryResolveSequenceByIndex(settings, index, out var sequencePath, out var sequenceName))
        {
            Debug.LogError($"Sequence loading helper failed: index {index} is out of range for {directories.Length} sequences.");
            return;
        }

        Debug.Log($"Loading sequence by helper: [{index}] {sequenceName}");
        sequenceUI.sequence.Load(sequencePath, sequenceName);
    }

    private void EnsureSettingsLoaded()
    {
        if (settings != null && !string.IsNullOrWhiteSpace(settings.dataPath))
            return;

        Debug.Log("Loading settings from " + Directory.GetCurrentDirectory() + settingsPath);
        settings = sequenceCatalog.LoadOrCreateSettings(settingsPath);
    }

}
