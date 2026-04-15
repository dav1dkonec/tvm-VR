using System;
using System.IO;

namespace TvmVr2.Client.Sequence
{
    public sealed class SequenceCatalog
    {
        public Settings LoadOrCreateSettings(string settingsPath)
        {
            var dataPath = Directory.GetCurrentDirectory();
            var absolutePath = dataPath + settingsPath;

            var settings = File.Exists(absolutePath)
                ? Serialization.Deserialize<Settings>(absolutePath)
                : new Settings();

            Serialization.Serialize(settings, absolutePath);
            return settings;
        }

        public string[] GetSequenceDirectories(Settings settings)
        {
            if (settings == null || string.IsNullOrWhiteSpace(settings.dataPath) || !Directory.Exists(settings.dataPath))
                return Array.Empty<string>();

            var directories = Directory.GetDirectories(settings.dataPath);
            Array.Sort(directories, StringComparer.Ordinal);
            return directories;
        }

        public bool TryResolveSequenceByIndex(Settings settings, int index, out string sequencePath, out string sequenceName)
        {
            sequencePath = null;
            sequenceName = null;

            var directories = GetSequenceDirectories(settings);
            if (index < 0 || index >= directories.Length)
                return false;

            sequencePath = directories[index];
            sequenceName = Path.GetFileName(sequencePath);
            return true;
        }
    }
}
