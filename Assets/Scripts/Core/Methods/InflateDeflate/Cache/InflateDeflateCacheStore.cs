using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TvmVr2.Core.Methods.InflateDeflate.Cache
{
    /// <summary>
    /// Metadata describing one inflate/deflate cache.
    /// </summary>
    public sealed class InflateDeflateCacheManifest
    {
        /// <summary>
        /// Current cache schema version.
        /// </summary>
        public const int CurrentSchemaVersion = 1;

        /// <summary>
        /// Cache schema version.
        /// </summary>
        public int SchemaVersion { get; set; } = CurrentSchemaVersion;

        /// <summary>
        /// Cached sequence identifier.
        /// </summary>
        public string SequenceId { get; set; } = string.Empty;

        /// <summary>
        /// Cached frame count.
        /// </summary>
        public int FrameCount { get; set; }

        /// <summary>
        /// Cached center count.
        /// </summary>
        public int CenterCount { get; set; }

        /// <summary>
        /// Cached vertex count.
        /// </summary>
        public int VertexCount { get; set; }

        /// <summary>
        /// Cached face count.
        /// </summary>
        public int FaceCount { get; set; }

        /// <summary>
        /// Surface neighbor count.
        /// </summary>
        public int Neighbors { get; set; }

        /// <summary>
        /// Surface deformation shape parameter.
        /// </summary>
        public float Shape { get; set; }

        /// <summary>
        /// Surface deformation limit epsilon.
        /// </summary>
        public float LimitEpsilon { get; set; }

        /// <summary>
        /// Maximum surface split iterations.
        /// </summary>
        public int MaxSplitIterations { get; set; }

        /// <summary>
        /// Affinity distance shape parameter.
        /// </summary>
        public float AffinityShapeDistance { get; set; }

        /// <summary>
        /// Affinity direction shape parameter.
        /// </summary>
        public float AffinityShapeDirection { get; set; }

        /// <summary>
        /// Affinity power parameter.
        /// </summary>
        public int AffinityPower { get; set; }

        /// <summary>
        /// Hash of source sequence data.
        /// </summary>
        public string SourceHash { get; set; } = string.Empty;

        /// <summary>
        /// Cache creation time in UTC ticks.
        /// </summary>
        public long GeneratedAtUtcTicks { get; set; }

        /// <summary>
        /// Checks cache compatibility.
        /// </summary>
        public bool IsCompatibleWith(InflateDeflateCacheManifest other)
        {
            if (other == null)
                return false;

            return SchemaVersion == other.SchemaVersion
                && string.Equals(SequenceId, other.SequenceId, StringComparison.Ordinal)
                && FrameCount == other.FrameCount
                && CenterCount == other.CenterCount
                && VertexCount == other.VertexCount
                && FaceCount == other.FaceCount
                && Neighbors == other.Neighbors
                && Shape.Equals(other.Shape)
                && LimitEpsilon.Equals(other.LimitEpsilon)
                && MaxSplitIterations == other.MaxSplitIterations
                && AffinityShapeDistance.Equals(other.AffinityShapeDistance)
                && AffinityShapeDirection.Equals(other.AffinityShapeDirection)
                && AffinityPower == other.AffinityPower
                && string.Equals(SourceHash, other.SourceHash, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Cached surface deformation data for one frame.
    /// </summary>
    public sealed class InflateDeflateSurfaceFrameCache
    {
        /// <summary>
        /// Cached frame index.
        /// </summary>
        public int FrameIndex { get; set; }

        /// <summary>
        /// Nearest centers for vertices.
        /// </summary>
        public int[][] Centers { get; set; } = Array.Empty<int[]>();

        /// <summary>
        /// Surface deformation weights.
        /// </summary>
        public float[][] Weights { get; set; } = Array.Empty<float[]>();
    }

    /// <summary>
    /// Cached Kabsch propagation data for one frame.
    /// </summary>
    public sealed class InflateDeflateKabschFrameCache
    {
        /// <summary>
        /// Cached frame index.
        /// </summary>
        public int FrameIndex { get; set; }

        /// <summary>
        /// Neighbor weights matrix.
        /// </summary>
        public float[,] NeighborWeights { get; set; }

        /// <summary>
        /// Sums of neighbor weights.
        /// </summary>
        public float[] NeighborWeightsSums { get; set; } = Array.Empty<float>();
    }

    /// <summary>
    /// Loaded inflate/deflate cache bundle.
    /// </summary>
    public sealed class InflateDeflateCacheBundle
    {
        /// <summary>
        /// Cache manifest.
        /// </summary>
        public InflateDeflateCacheManifest Manifest { get; set; } = new InflateDeflateCacheManifest();

        /// <summary>
        /// Cached center affinity matrix.
        /// </summary>
        public float[,] Affinity { get; set; }

        /// <summary>
        /// Cached structural neighbor indices.
        /// </summary>
        public int[,] NeighborIndices { get; set; }

        /// <summary>
        /// Cached surface data by frame.
        /// </summary>
        public Dictionary<int, InflateDeflateSurfaceFrameCache> SurfaceFrameCaches { get; } = new Dictionary<int, InflateDeflateSurfaceFrameCache>();

        /// <summary>
        /// Cached Kabsch data by frame.
        /// </summary>
        public Dictionary<int, InflateDeflateKabschFrameCache> KabschFrameCaches { get; } = new Dictionary<int, InflateDeflateKabschFrameCache>();
    }

    /// <summary>
    /// Reads and writes inflate/deflate cache files.
    /// </summary>
    public static class InflateDeflateCacheStore
    {
        private const string ManifestFileName = "manifest.bin";
        private const string AffinityFileName = "affinity.bin";
        private const string StructuralFileName = "structural.bin";
        private const string SurfaceDirectoryName = "surface";
        private const string KabschDirectoryName = "kabsch";

        /// <summary>
        /// Gets cache directory path for a sequence.
        /// </summary>
        public static string GetSequenceDirectoryPath(string rootPath, string sequenceId)
        {
            return Path.Combine(rootPath ?? string.Empty, NormalizeSegment(sequenceId));
        }

        /// <summary>
        /// Gets manifest file path for a sequence.
        /// </summary>
        public static string GetManifestPath(string rootPath, string sequenceId)
        {
            return Path.Combine(GetSequenceDirectoryPath(rootPath, sequenceId), ManifestFileName);
        }

        /// <summary>
        /// Tries to load a cache bundle.
        /// </summary>
        public static bool TryLoadBundle(string rootPath, string sequenceId, out InflateDeflateCacheBundle bundle, out string errorMessage)
        {
            bundle = null;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(rootPath))
            {
                errorMessage = "InflateDeflate cache root path is missing.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(sequenceId))
            {
                errorMessage = "InflateDeflate cache sequence id is missing.";
                return false;
            }

            var sequenceDirectory = GetSequenceDirectoryPath(rootPath, sequenceId);
            if (!Directory.Exists(sequenceDirectory))
            {
                errorMessage = $"InflateDeflate cache directory was not found: {sequenceDirectory}";
                return false;
            }

            var manifestPath = Path.Combine(sequenceDirectory, ManifestFileName);
            if (!File.Exists(manifestPath))
            {
                errorMessage = $"InflateDeflate cache manifest was not found: {manifestPath}";
                return false;
            }

            try
            {
                using var manifestStream = new FileStream(manifestPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var manifestReader = new BinaryReader(manifestStream, Encoding.UTF8, leaveOpen: false);

                var manifest = ReadManifest(manifestReader);
                if (!string.Equals(manifest.SequenceId, sequenceId, StringComparison.Ordinal))
                {
                    errorMessage = $"InflateDeflate cache manifest sequence id mismatch: {manifest.SequenceId} != {sequenceId}";
                    return false;
                }

                bundle = new InflateDeflateCacheBundle
                {
                    Manifest = manifest,
                    Affinity = ReadFloatMatrix(Path.Combine(sequenceDirectory, AffinityFileName)),
                    NeighborIndices = ReadIntMatrix(Path.Combine(sequenceDirectory, StructuralFileName))
                };

                var surfaceDirectory = Path.Combine(sequenceDirectory, SurfaceDirectoryName);
                if (Directory.Exists(surfaceDirectory))
                {
                    for (var frameIndex = 0; frameIndex < manifest.FrameCount; frameIndex++)
                    {
                        var framePath = Path.Combine(surfaceDirectory, BuildFrameFileName(frameIndex));
                        if (!File.Exists(framePath))
                        {
                            errorMessage = $"InflateDeflate surface cache frame was not found: {framePath}";
                            return false;
                        }

                        bundle.SurfaceFrameCaches[frameIndex] = ReadSurfaceFrameCache(framePath);
                    }
                }
                else
                {
                    errorMessage = $"InflateDeflate surface cache directory was not found: {surfaceDirectory}";
                    return false;
                }

                var kabschDirectory = Path.Combine(sequenceDirectory, KabschDirectoryName);
                if (Directory.Exists(kabschDirectory))
                {
                    for (var frameIndex = 0; frameIndex < manifest.FrameCount; frameIndex++)
                    {
                        var framePath = Path.Combine(kabschDirectory, BuildFrameFileName(frameIndex));
                        if (!File.Exists(framePath))
                        {
                            errorMessage = $"InflateDeflate kabsch cache frame was not found: {framePath}";
                            return false;
                        }

                        bundle.KabschFrameCaches[frameIndex] = ReadKabschFrameCache(framePath);
                    }
                }
                else
                {
                    errorMessage = $"InflateDeflate kabsch cache directory was not found: {kabschDirectory}";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.ToString();
                bundle = null;
                return false;
            }
        }

        /// <summary>
        /// Tries to save a cache bundle.
        /// </summary>
        public static bool TrySaveBundle(string rootPath, InflateDeflateCacheBundle bundle, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (bundle?.Manifest == null)
            {
                errorMessage = "InflateDeflate cache bundle is missing manifest.";
                return false;
            }

            var sequenceDirectory = GetSequenceDirectoryPath(rootPath, bundle.Manifest.SequenceId);
            var surfaceDirectory = Path.Combine(sequenceDirectory, SurfaceDirectoryName);
            var kabschDirectory = Path.Combine(sequenceDirectory, KabschDirectoryName);

            try
            {
                Directory.CreateDirectory(sequenceDirectory);
                Directory.CreateDirectory(surfaceDirectory);
                Directory.CreateDirectory(kabschDirectory);

                using (var manifestStream = new FileStream(Path.Combine(sequenceDirectory, ManifestFileName), FileMode.Create, FileAccess.Write, FileShare.None))
                using (var manifestWriter = new BinaryWriter(manifestStream, Encoding.UTF8, leaveOpen: false))
                {
                    WriteManifest(manifestWriter, bundle.Manifest);
                }

                WriteFloatMatrix(Path.Combine(sequenceDirectory, AffinityFileName), bundle.Affinity);
                WriteIntMatrix(Path.Combine(sequenceDirectory, StructuralFileName), bundle.NeighborIndices);

                if (bundle.Manifest.FrameCount > 0)
                {
                    for (var frameIndex = 0; frameIndex < bundle.Manifest.FrameCount; frameIndex++)
                    {
                        if (!bundle.SurfaceFrameCaches.TryGetValue(frameIndex, out var surfaceFrameCache))
                        {
                            errorMessage = $"InflateDeflate surface cache is missing frame {frameIndex}.";
                            return false;
                        }

                        WriteSurfaceFrameCache(Path.Combine(surfaceDirectory, BuildFrameFileName(frameIndex)), surfaceFrameCache);

                        if (!bundle.KabschFrameCaches.TryGetValue(frameIndex, out var kabschFrameCache))
                        {
                            errorMessage = $"InflateDeflate kabsch cache is missing frame {frameIndex}.";
                            return false;
                        }

                        WriteKabschFrameCache(Path.Combine(kabschDirectory, BuildFrameFileName(frameIndex)), kabschFrameCache);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.ToString();
                return false;
            }
        }

        private static InflateDeflateCacheManifest ReadManifest(BinaryReader reader)
        {
            return new InflateDeflateCacheManifest
            {
                SchemaVersion = reader.ReadInt32(),
                SequenceId = ReadString(reader),
                FrameCount = reader.ReadInt32(),
                CenterCount = reader.ReadInt32(),
                VertexCount = reader.ReadInt32(),
                FaceCount = reader.ReadInt32(),
                Neighbors = reader.ReadInt32(),
                Shape = reader.ReadSingle(),
                LimitEpsilon = reader.ReadSingle(),
                MaxSplitIterations = reader.ReadInt32(),
                AffinityShapeDistance = reader.ReadSingle(),
                AffinityShapeDirection = reader.ReadSingle(),
                AffinityPower = reader.ReadInt32(),
                SourceHash = ReadString(reader),
                GeneratedAtUtcTicks = reader.ReadInt64()
            };
        }

        private static void WriteManifest(BinaryWriter writer, InflateDeflateCacheManifest manifest)
        {
            writer.Write(manifest.SchemaVersion);
            WriteString(writer, manifest.SequenceId);
            writer.Write(manifest.FrameCount);
            writer.Write(manifest.CenterCount);
            writer.Write(manifest.VertexCount);
            writer.Write(manifest.FaceCount);
            writer.Write(manifest.Neighbors);
            writer.Write(manifest.Shape);
            writer.Write(manifest.LimitEpsilon);
            writer.Write(manifest.MaxSplitIterations);
            writer.Write(manifest.AffinityShapeDistance);
            writer.Write(manifest.AffinityShapeDirection);
            writer.Write(manifest.AffinityPower);
            WriteString(writer, manifest.SourceHash);
            writer.Write(manifest.GeneratedAtUtcTicks);
        }

        private static InflateDeflateSurfaceFrameCache ReadSurfaceFrameCache(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: false);

            return new InflateDeflateSurfaceFrameCache
            {
                FrameIndex = reader.ReadInt32(),
                Centers = ReadJaggedIntArray(reader),
                Weights = ReadJaggedFloatArray(reader)
            };
        }

        private static void WriteSurfaceFrameCache(string path, InflateDeflateSurfaceFrameCache cache)
        {
            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: false);

            writer.Write(cache.FrameIndex);
            WriteJaggedIntArray(writer, cache.Centers);
            WriteJaggedFloatArray(writer, cache.Weights);
        }

        private static InflateDeflateKabschFrameCache ReadKabschFrameCache(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: false);

            return new InflateDeflateKabschFrameCache
            {
                FrameIndex = reader.ReadInt32(),
                NeighborWeights = ReadFloatMatrix(reader),
                NeighborWeightsSums = ReadFloatArray(reader)
            };
        }

        private static void WriteKabschFrameCache(string path, InflateDeflateKabschFrameCache cache)
        {
            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: false);

            writer.Write(cache.FrameIndex);
            WriteFloatMatrix(writer, cache.NeighborWeights);
            WriteFloatArray(writer, cache.NeighborWeightsSums);
        }

        private static float[,] ReadFloatMatrix(string path)
        {
            if (!File.Exists(path))
                return null;

            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: false);
            return ReadFloatMatrix(reader);
        }

        private static void WriteFloatMatrix(string path, float[,] matrix)
        {
            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: false);
            WriteFloatMatrix(writer, matrix);
        }

        private static int[,] ReadIntMatrix(string path)
        {
            if (!File.Exists(path))
                return null;

            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: false);
            return ReadIntMatrix(reader);
        }

        private static void WriteIntMatrix(string path, int[,] matrix)
        {
            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: false);
            WriteIntMatrix(writer, matrix);
        }

        private static float[,] ReadFloatMatrix(BinaryReader reader)
        {
            var rows = reader.ReadInt32();
            var columns = reader.ReadInt32();
            if (rows < 0 || columns < 0)
                return null;

            var matrix = new float[rows, columns];
            for (var r = 0; r < rows; r++)
            {
                for (var c = 0; c < columns; c++)
                    matrix[r, c] = reader.ReadSingle();
            }

            return matrix;
        }

        private static void WriteFloatMatrix(BinaryWriter writer, float[,] matrix)
        {
            if (matrix == null)
            {
                writer.Write(-1);
                writer.Write(-1);
                return;
            }

            var rows = matrix.GetLength(0);
            var columns = matrix.GetLength(1);
            writer.Write(rows);
            writer.Write(columns);
            for (var r = 0; r < rows; r++)
            {
                for (var c = 0; c < columns; c++)
                    writer.Write(matrix[r, c]);
            }
        }

        private static int[,] ReadIntMatrix(BinaryReader reader)
        {
            var rows = reader.ReadInt32();
            var columns = reader.ReadInt32();
            if (rows < 0 || columns < 0)
                return null;

            var matrix = new int[rows, columns];
            for (var r = 0; r < rows; r++)
            {
                for (var c = 0; c < columns; c++)
                    matrix[r, c] = reader.ReadInt32();
            }

            return matrix;
        }

        private static void WriteIntMatrix(BinaryWriter writer, int[,] matrix)
        {
            if (matrix == null)
            {
                writer.Write(-1);
                writer.Write(-1);
                return;
            }

            var rows = matrix.GetLength(0);
            var columns = matrix.GetLength(1);
            writer.Write(rows);
            writer.Write(columns);
            for (var r = 0; r < rows; r++)
            {
                for (var c = 0; c < columns; c++)
                    writer.Write(matrix[r, c]);
            }
        }

        private static float[] ReadFloatArray(BinaryReader reader)
        {
            var length = reader.ReadInt32();
            if (length < 0)
                return null;

            var values = new float[length];
            for (var i = 0; i < length; i++)
                values[i] = reader.ReadSingle();

            return values;
        }

        private static void WriteFloatArray(BinaryWriter writer, float[] values)
        {
            if (values == null)
            {
                writer.Write(-1);
                return;
            }

            writer.Write(values.Length);
            for (var i = 0; i < values.Length; i++)
                writer.Write(values[i]);
        }

        private static int[][] ReadJaggedIntArray(BinaryReader reader)
        {
            var outerLength = reader.ReadInt32();
            if (outerLength < 0)
                return null;

            var result = new int[outerLength][];
            for (var i = 0; i < outerLength; i++)
            {
                var innerLength = reader.ReadInt32();
                if (innerLength < 0)
                {
                    result[i] = null;
                    continue;
                }

                result[i] = new int[innerLength];
                for (var j = 0; j < innerLength; j++)
                    result[i][j] = reader.ReadInt32();
            }

            return result;
        }

        private static void WriteJaggedIntArray(BinaryWriter writer, int[][] values)
        {
            if (values == null)
            {
                writer.Write(-1);
                return;
            }

            writer.Write(values.Length);
            for (var i = 0; i < values.Length; i++)
            {
                if (values[i] == null)
                {
                    writer.Write(-1);
                    continue;
                }

                writer.Write(values[i].Length);
                for (var j = 0; j < values[i].Length; j++)
                    writer.Write(values[i][j]);
            }
        }

        private static float[][] ReadJaggedFloatArray(BinaryReader reader)
        {
            var outerLength = reader.ReadInt32();
            if (outerLength < 0)
                return null;

            var result = new float[outerLength][];
            for (var i = 0; i < outerLength; i++)
            {
                var innerLength = reader.ReadInt32();
                if (innerLength < 0)
                {
                    result[i] = null;
                    continue;
                }

                result[i] = new float[innerLength];
                for (var j = 0; j < innerLength; j++)
                    result[i][j] = reader.ReadSingle();
            }

            return result;
        }

        private static void WriteJaggedFloatArray(BinaryWriter writer, float[][] values)
        {
            if (values == null)
            {
                writer.Write(-1);
                return;
            }

            writer.Write(values.Length);
            for (var i = 0; i < values.Length; i++)
            {
                if (values[i] == null)
                {
                    writer.Write(-1);
                    continue;
                }

                writer.Write(values[i].Length);
                for (var j = 0; j < values[i].Length; j++)
                    writer.Write(values[i][j]);
            }
        }

        private static string ReadString(BinaryReader reader)
        {
            var hasValue = reader.ReadBoolean();
            return hasValue ? reader.ReadString() : string.Empty;
        }

        private static void WriteString(BinaryWriter writer, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                writer.Write(false);
                return;
            }

            writer.Write(true);
            writer.Write(value);
        }

        private static string BuildFrameFileName(int frameIndex)
        {
            return $"frame_{frameIndex:D3}.bin";
        }

        private static string NormalizeSegment(string segment)
        {
            if (string.IsNullOrWhiteSpace(segment))
                return "_";

            var invalidChars = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder(segment.Length);
            foreach (var ch in segment)
            {
                builder.Append(invalidChars.Contains(ch) ? '_' : ch);
            }

            return builder.ToString();
        }
    }
}
