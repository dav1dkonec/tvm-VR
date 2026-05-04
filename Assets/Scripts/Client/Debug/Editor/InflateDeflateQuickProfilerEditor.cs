using TvmVr2.Client.DebugTools;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(InflateDeflateQuickProfiler))]
public sealed class InflateDeflateQuickProfilerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var profiler = (InflateDeflateQuickProfiler)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Catalog Sequences", EditorStyles.boldLabel);

        if (!Application.isPlaying)
            EditorGUILayout.HelpBox("Sequence load buttons are available in Play Mode.", MessageType.Info);

        var sequenceNames = InflateDeflateQuickProfiler.GetCatalogSequenceNames();

        if (sequenceNames == null || sequenceNames.Length == 0)
        {
            EditorGUILayout.HelpBox("No sequences were found in the configured catalog.", MessageType.Warning);
            return;
        }

        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            for (var i = 0; i < sequenceNames.Length; i++)
            {
                var sequenceName = sequenceNames[i];
                if (GUILayout.Button($"Load {sequenceName}"))
                    profiler.LoadSequenceByName(sequenceName);
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Move Sequence To Camera"))
                profiler.MoveSequenceToCamera();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Visible Debug Apply", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Applies inflate/deflate directly to the loaded sequence in Play Mode so the mesh stays visibly edited without a VR headset.",
                MessageType.Info);

            DrawRegionButtons(profiler, InflateDeflateQuickProfiler.DebugBodyRegion.Head, "Head");
            DrawRegionButtons(profiler, InflateDeflateQuickProfiler.DebugBodyRegion.Belly, "Belly");
            DrawRegionButtons(profiler, InflateDeflateQuickProfiler.DebugBodyRegion.Arm, "Arm");
            DrawRegionButtons(profiler, InflateDeflateQuickProfiler.DebugBodyRegion.Leg, "Leg");
        }
    }

    private static void DrawRegionButtons(
        InflateDeflateQuickProfiler profiler,
        InflateDeflateQuickProfiler.DebugBodyRegion region,
        string label)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button($"Inflate {label}"))
                profiler.ApplyRegionDebugEdit(region, TvmVr2.Api.Enums.InflateDeflateMode.Inflate);

            if (GUILayout.Button($"Deflate {label}"))
                profiler.ApplyRegionDebugEdit(region, TvmVr2.Api.Enums.InflateDeflateMode.Deflate);
        }
    }
}
