using System.Collections.Generic;
using System.Linq;

public sealed class RuntimeTestResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string PipelineName { get; set; } = string.Empty;
    public string SequenceName { get; set; } = string.Empty;
    public int FrameIndex { get; set; }
    public int[] EditedCenterIndices { get; set; } = new int[0];
    public float EditedCenterErrorMean { get; set; }
    public float EditedCenterErrorMax { get; set; }
    public IReadOnlyList<int> AffectedFrames { get; set; } = new int[0];
    public float LocalCenterDisplacementMean { get; set; }
    public float LocalCenterDisplacementMax { get; set; }
    public float LocalMeshDisplacementMean { get; set; }
    public float LocalMeshDisplacementMax { get; set; }
    public float TotalRuntimeMilliseconds { get; set; }
    public float AffinityMilliseconds { get; set; }
    public float CenterMilliseconds { get; set; }
    public float PropagationMilliseconds { get; set; }
    public float SurfaceMilliseconds { get; set; }
    public System.Numerics.Vector3[] BeforePositions { get; set; } = new System.Numerics.Vector3[0];
    public System.Numerics.Vector3[] RequestedPositions { get; set; } = new System.Numerics.Vector3[0];
    public System.Numerics.Vector3[] AfterPositions { get; set; } = new System.Numerics.Vector3[0];

    public string ToLogMessage()
    {
        var centersLabel = EditedCenterIndices.Length == 1
            ? $"center {EditedCenterIndices[0]}"
            : $"edited centers {string.Join(",", EditedCenterIndices)}";

        var positionSummary = EditedCenterIndices.Length == 1 && BeforePositions.Length == 1 && RequestedPositions.Length == 1 && AfterPositions.Length == 1
            ? $"Center before/requested/after: ({BeforePositions[0].X:F4}, {BeforePositions[0].Y:F4}, {BeforePositions[0].Z:F4}) / " +
              $"({RequestedPositions[0].X:F4}, {RequestedPositions[0].Y:F4}, {RequestedPositions[0].Z:F4}) / " +
              $"({AfterPositions[0].X:F4}, {AfterPositions[0].Y:F4}, {AfterPositions[0].Z:F4}). "
            : string.Empty;

        var affectedRange = AffectedFrames.Count == 0
            ? "none"
            : $"{AffectedFrames[0]}-{AffectedFrames[AffectedFrames.Count - 1]}";

        var centerErrorLabel = EditedCenterIndices.Length == 1
            ? $"Edited center error: {EditedCenterErrorMax:F6}. "
            : $"Edited center error mean/max: {EditedCenterErrorMean:F6}/{EditedCenterErrorMax:F6}. ";

        var breakdown = PipelineName == "UNIFIED"
            ? $"Unified breakdown affinity/center/propagation/surface: {AffinityMilliseconds:F3}/{CenterMilliseconds:F3}/{PropagationMilliseconds:F3}/{SurfaceMilliseconds:F3} ms."
            : $"Legacy breakdown center/propagation/surface: {CenterMilliseconds:F3}/{PropagationMilliseconds:F3}/{SurfaceMilliseconds:F3} ms.";

        return
            $"{(EditedCenterIndices.Length == 1 ? "Editor commit" : "Multi-center editor commit")} finished using {PipelineName} pipeline on sequence '{SequenceName}', frame {FrameIndex}, {centersLabel}. " +
            positionSummary +
            centerErrorLabel +
            $"Affected frames: {AffectedFrames.Count}/{affectedRange}. " +
            $"Local center displacement mean/max: {LocalCenterDisplacementMean:F6}/{LocalCenterDisplacementMax:F6}. " +
            $"Local mesh displacement mean/max: {LocalMeshDisplacementMean:F6}/{LocalMeshDisplacementMax:F6}. " +
            $"Total runtime: {TotalRuntimeMilliseconds:F3} ms. " +
            breakdown;
    }
}
