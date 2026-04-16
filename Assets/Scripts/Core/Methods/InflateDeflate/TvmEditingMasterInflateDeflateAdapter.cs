using System.Linq;
using System.Numerics;
using TvmVr2.Core.Abstractions;
using TvmVr2.Core.Common;
using TVMEditor.Editing;
using TVMEditor.Editing.AffinityCalculation;
using TVMEditor.Editing.CenterDeformation;
using TVMEditor.Editing.SurfaceDeformation;
using TVMEditor.Editing.TransformPropagation;
using TVMEditor.Structures;

namespace TvmVr2.Core.Methods.InflateDeflate
{
    public sealed class TvmEditingMasterInflateDeflateAdapter
    {
        public IMethodResult Execute(InflateDeflateMethodInput input)
        {
            if (input == null)
            {
                return MethodExecutionResult.NotImplemented("InflateDeflate input is missing.");
            }

            if (input.Frames == null || input.Frames.Length == 0)
            {
                return MethodExecutionResult.NotImplemented("InflateDeflate runtime data are missing.");
            }

            if (input.SelectedCenterIndices == null || input.CenterTranslations == null)
            {
                return MethodExecutionResult.NotImplemented("InflateDeflate effectors were not resolved.");
            }

            if (input.SelectedCenterIndices.Length != input.CenterTranslations.Length)
            {
                return MethodExecutionResult.NotImplemented("InflateDeflate effectors are inconsistent.");
            }

            if (input.SelectedCenterIndices.Length == 0)
            {
                return new MethodExecutionResult
                {
                    Success = false,
                    ErrorMessage = "InflateDeflate resolved no affected centers for the current request."
                };
            }

            var sequence = new TriangleMeshSequence
            {
                Meshes = input.Frames.Select(frame => new TriangleMesh
                {
                    Vertices = frame.vertices,
                    Faces = frame.faces.Select(face => new TVMEditor.Structures.Face(face.V1, face.V2, face.V3)).ToArray()
                }).ToArray()
            };

            var centers = input.Frames.Select(frame => frame.centers).ToArray();
            var transformations = Enumerable
                .Repeat(DualQuaternion.Identity(), centers[input.FrameIndex].Length)
                .ToArray();

            for (var i = 0; i < input.SelectedCenterIndices.Length; i++)
            {
                var centerIndex = input.SelectedCenterIndices[i];
                if (centerIndex < 0 || centerIndex >= transformations.Length)
                {
                    return new MethodExecutionResult
                    {
                        Success = false,
                        ErrorMessage = $"InflateDeflate selected center index {centerIndex} is outside valid range 0..{transformations.Length - 1}."
                    };
                }

                var translation = input.CenterTranslations[i];
                transformations[centerIndex] = DualQuaternion.Translation(new Vector3(translation.X, translation.Y, translation.Z));
            }

            var affinityCalculation = new DistanceDirectionAffinityCalculation();
            var meshEditor = new MeshEditor(
                affinityCalculation,
                new AffinityCenterDeformation(affinityCalculation),
                null,
                new CustomSurfaceDeformation(affinityCalculation),
                new KabschTransformPropagation(affinityCalculation));

            meshEditor.Deform(
                sequence,
                centers,
                input.SelectedCenterIndices,
                transformations,
                input.FrameIndex,
                out var deformedSequence,
                out var deformedCenters);

            for (var i = 0; i < input.Frames.Length; i++)
            {
                input.Frames[i].centers = deformedCenters[i];
                input.Frames[i].vertices = deformedSequence.Meshes[i].Vertices;
            }

            return new MethodExecutionResult
            {
                Success = true,
                ErrorMessage = string.Empty
            };
        }
    }
}
