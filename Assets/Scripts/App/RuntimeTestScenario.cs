using UnityEngine;

public enum RuntimeTestKind
{
    SingleCenter,
    MultiCenter
}

public sealed class RuntimeTestScenario
{
    public RuntimeTestKind Kind { get; set; }
    public bool UseUnifiedPipeline { get; set; }
    public int FrameIndex { get; set; }
    public int[] CenterIndices { get; set; } = new int[0];
    public Vector3[] Translations { get; set; } = new Vector3[0];
    public int RepeatCount { get; set; } = 1;

    public static RuntimeTestScenario CreateSingle(bool useUnifiedPipeline, int frameIndex, int centerIndex, Vector3 translation)
    {
        return new RuntimeTestScenario
        {
            Kind = RuntimeTestKind.SingleCenter,
            UseUnifiedPipeline = useUnifiedPipeline,
            FrameIndex = frameIndex,
            CenterIndices = new[] { centerIndex },
            Translations = new[] { translation },
            RepeatCount = 1
        };
    }

    public static RuntimeTestScenario CreateMulti(bool useUnifiedPipeline, int frameIndex, int[] centerIndices, Vector3[] translations)
    {
        return new RuntimeTestScenario
        {
            Kind = RuntimeTestKind.MultiCenter,
            UseUnifiedPipeline = useUnifiedPipeline,
            FrameIndex = frameIndex,
            CenterIndices = centerIndices ?? new int[0],
            Translations = translations ?? new Vector3[0],
            RepeatCount = 1
        };
    }
}
