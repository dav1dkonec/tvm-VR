using System;
using System.IO;
using System.Threading.Tasks;
using TvmVr2.Api.Sequence;

namespace TvmVr2.Client.Sequence
{
    /// <summary>
    /// Saves edited sequence data to disk.
    /// </summary>
    public sealed class SequenceSaver
    {
        /// <summary>
        /// Builds output directory path.
        /// </summary>
        public string BuildOutputDirectoryPath(string baseDirectoryPath, string sequenceName, DateTime timestamp)
        {
            var dirname = sequenceName + "_" + timestamp.ToString("yyyyMMddHHmmssfff");
            return Path.Combine(baseDirectoryPath, dirname);
        }

        /// <summary>
        /// Saves sequence data asynchronously.
        /// </summary>
        public async Task<SequenceSaveResult> SaveAsync(SequenceSaveRequest request)
        {
            if (request == null)
            {
                return new SequenceSaveResult
                {
                    Success = false,
                    ErrorMessage = "Sequence save request is null."
                };
            }

            if (string.IsNullOrWhiteSpace(request.BaseDirectoryPath) || !Directory.Exists(request.BaseDirectoryPath))
            {
                return new SequenceSaveResult
                {
                    Success = false,
                    ErrorMessage = "Base directory path is missing or does not exist."
                };
            }

            if (request.Frames == null || request.Frames.Length == 0)
            {
                return new SequenceSaveResult
                {
                    Success = false,
                    ErrorMessage = "There are no frames to save."
                };
            }

            var timestamp = request.Timestamp == default ? DateTime.Now : request.Timestamp;
            var outputDirectoryPath = BuildOutputDirectoryPath(request.BaseDirectoryPath, request.SequenceName, timestamp);
            var centersPath = Path.Combine(outputDirectoryPath, "centers");
            var meshesPath = Path.Combine(outputDirectoryPath, "meshes");
            var settingsPath = Path.Combine(outputDirectoryPath, "settings.xml");

            Directory.CreateDirectory(outputDirectoryPath);
            Directory.CreateDirectory(centersPath);
            Directory.CreateDirectory(meshesPath);

            var settings = new SequenceSettings();
            Serialization.Serialize(settings, settingsPath);

            await Task.Run(() =>
            {
                for (var i = 0; i < request.Frames.Length; i++)
                {
                    CentersIO.WriteXYZ(Path.Combine(centersPath, $"{i:0000}.xyz"), request.Frames[i].centers);
                    MeshIO.WriteMesh(request.Frames[i].vertices, request.Frames[i].faces, Path.Combine(meshesPath, $"{i:0000}.obj"));
                }
            });

            return new SequenceSaveResult
            {
                Success = true,
                OutputDirectoryPath = outputDirectoryPath
            };
        }
    }
}
