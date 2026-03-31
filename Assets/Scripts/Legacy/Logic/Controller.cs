using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Initializes the application
/// </summary>
public class Controller : MonoBehaviour
{
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

        EnsureSettingsLoaded();

        // Load sequence list
        if (Directory.Exists(settings.dataPath))
        {
            var dirs = Directory.GetDirectories(settings.dataPath);
            foreach (var d in dirs)
            {
                var name = Path.GetFileName(d);
                sequenceUI.Add(name, d);
                Debug.Log("Sequence available: " + name);
            }
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

        if (!Directory.Exists(settings.dataPath))
        {
            Debug.LogError("Sequence loading helper failed: data path does not exist: " + settings.dataPath);
            return;
        }

        var dirs = Directory.GetDirectories(settings.dataPath);
        if (dirs.Length == 0)
        {
            Debug.LogError("Sequence loading helper failed: no sequences found in " + settings.dataPath);
            return;
        }

        if (index < 0 || index >= dirs.Length)
        {
            Debug.LogError($"Sequence loading helper failed: index {index} is out of range for {dirs.Length} sequences.");
            return;
        }

        var sequencePath = dirs[index];
        var sequenceName = Path.GetFileName(sequencePath);
        Debug.Log($"Loading sequence by helper: [{index}] {sequenceName}");
        sequenceUI.sequence.Load(sequencePath, sequenceName);
    }

    private void EnsureSettingsLoaded()
    {
        var dataPath = Directory.GetCurrentDirectory();
        var path = dataPath + settingsPath;

        if (settings != null && !string.IsNullOrWhiteSpace(settings.dataPath))
            return;

        Debug.Log("Loading settings from " + path);
        settings = File.Exists(path)
            ? Serialization.Deserialize<Settings>(dataPath + settingsPath)
            : new();
        Serialization.Serialize(settings, path);
    }

}
