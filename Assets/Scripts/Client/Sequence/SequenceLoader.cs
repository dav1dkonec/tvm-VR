using System.IO;
using System.Threading.Tasks;
using TvmVr2.Api.Sequence;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace TvmVr2.Client.Sequence
{
    /// <summary>
    /// Loads sequence data from disk.
    /// </summary>
    public sealed class SequenceLoader
    {
        /// <summary>
        /// Loads sequence data asynchronously.
        /// </summary>
        public async Task<SequenceLoadResult> LoadAsync(SequenceLoadRequest request)
        {
            if (request == null)
            {
                return new SequenceLoadResult
                {
                    Success = false,
                    ErrorMessage = "Sequence load request is null."
                };
            }

            var centersPath = Path.Combine(request.SequencePath, "centers");
            var meshesPath = Path.Combine(request.SequencePath, "meshes");
            var settingsPath = Path.Combine(request.SequencePath, "settings.xml");
            var totalTimer = Stopwatch.StartNew();

            var centersExist = Directory.Exists(centersPath);
            var meshesExist = Directory.Exists(meshesPath);
            var settingsExist = File.Exists(settingsPath);
            var centers = centersExist ? Directory.GetFiles(centersPath) : new string[0];
            var meshes = meshesExist ? Directory.GetFiles(meshesPath) : new string[0];

            var canLoad = centersExist && meshesExist && centers.Length == meshes.Length;
            if (!canLoad)
            {
                return new SequenceLoadResult
                {
                    Success = false,
                    ErrorMessage = "Failed to load sequence files: centers or meshes are missing or inconsistent."
                };
            }

            var loadedFrames = await Task.Run(() =>
            {
                var loadedCenters = CentersIO.LoadCentersFiles(centers);
                var loadedCentersUnedited = CentersIO.LoadCentersFiles(centers);

                var frames = new Frame[centers.Length];
                for (var i = 0; i < frames.Length; i++)
                {
                    frames[i] = new Frame
                    {
                        centers = loadedCenters[i],
                        centersUnedited = loadedCentersUnedited[i]
                    };

                    MeshIO.LoadMesh(meshes[i], out frames[i].vertices, out frames[i].faces);
                    frames[i].verticesUnedited = (System.Numerics.Vector3[])frames[i].vertices.Clone();
                    frames[i].FindNearest(request.NearestCenterCount);
                    UnityEngine.Debug.Log($"SequenceLoader: loaded frame {i + 1}/{frames.Length}");
                }

                return frames;
            });

            var settings = settingsExist
                ? Serialization.Deserialize<SequenceSettings>(settingsPath)
                : new SequenceSettings();
            Serialization.Serialize(settings, settingsPath);

            var sequenceData = new SequenceData
            {
                SequenceId = request.SequenceName,
                Settings = settings,
                Topology = SequenceTopology.FromFrames(loadedFrames),
                OriginalFrames = loadedFrames
            };

            totalTimer.Stop();
            UnityEngine.Debug.Log(
                $"SequenceLoader: completed load for '{request.SequenceName}' in {totalTimer.Elapsed.TotalMilliseconds:F2} ms " +
                $"(frames={loadedFrames.Length}, centersPathExists={centersExist}, meshesPathExists={meshesExist}, settingsPathExists={settingsExist}).");

            return new SequenceLoadResult
            {
                Success = true,
                Frames = loadedFrames,
                Settings = settings,
                SequenceData = sequenceData
            };
        }

        /// <summary>
        /// Loads sequence data.
        /// </summary>
        public SequenceLoadResult Load(SequenceLoadRequest request)
        {
            return LoadAsync(request).GetAwaiter().GetResult();
        }
    }
}
