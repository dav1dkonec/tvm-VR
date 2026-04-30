using TvmVr2.Client.DebugTools;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(InflateDeflateQuickProfiler))]
public sealed class InflateDeflateQuickProfilerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Catalog Sequences", EditorStyles.boldLabel);

        if (!Application.isPlaying)
            EditorGUILayout.HelpBox("Sequence load buttons are available in Play Mode.", MessageType.Info);

        var profiler = (InflateDeflateQuickProfiler)target;
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
            if (GUILayout.Button("Export InflateDeflate Cache"))
                profiler.ExportInflateDeflateCache();
        }
    }
}
