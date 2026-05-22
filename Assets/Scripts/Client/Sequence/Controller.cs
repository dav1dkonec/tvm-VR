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
        }
    }

    private void EnsureSettingsLoaded()
    {
        if (settings != null && !string.IsNullOrWhiteSpace(settings.dataPath))
            return;

        settings = sequenceCatalog.LoadOrCreateSettings(settingsPath);
    }

}
