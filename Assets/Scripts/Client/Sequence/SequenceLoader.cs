using System.IO;
using System.Threading.Tasks;
using TvmVr2.Api.Sequence;

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
            return await Task.Run(() => Load(request));
        }

        /// <summary>
        /// Loads sequence data.
        /// </summary>
        public SequenceLoadResult Load(SequenceLoadRequest request)
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

            var loadedCenters = CentersIO.LoadCentersFiles(centers);
            var loadedCentersUnedited = CentersIO.LoadCentersFiles(centers);

            var loadedFrames = new Frame[centers.Length];
            for (var i = 0; i < loadedFrames.Length; i++)
            {
                loadedFrames[i] = new Frame
                {
                    centers = loadedCenters[i],
                    centersUnedited = loadedCentersUnedited[i]
                };

                MeshIO.LoadMesh(meshes[i], out loadedFrames[i].vertices, out loadedFrames[i].faces);
                MeshIO.LoadMesh(meshes[i], out loadedFrames[i].verticesUnedited, out loadedFrames[i].faces);
                loadedFrames[i].FindNearest(request.NearestCenterCount);
            }

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

            return new SequenceLoadResult
            {
                Success = true,
                Frames = loadedFrames,
                Settings = settings,
                SequenceData = sequenceData
            };
        }
    }
}
