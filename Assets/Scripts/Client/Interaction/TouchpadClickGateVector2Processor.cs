using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;

/// <summary>
/// Gates a Vector2 XR primary axis so it only produces values while the same controller's
/// primary 2D axis click is pressed. The gate is only applied to touchpad-like controllers.
/// </summary>
[DisplayName("Touchpad Click Gate Vector2")]
public sealed class TouchpadClickGateVector2Processor : InputProcessor<Vector2>
{
    private const string ProcessorName = "TouchpadClickGateVector2";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterRuntime()
    {
        InputSystem.RegisterProcessor<TouchpadClickGateVector2Processor>(ProcessorName);
    }

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    private static void RegisterEditor()
    {
        InputSystem.RegisterProcessor<TouchpadClickGateVector2Processor>(ProcessorName);
    }
#endif

    public override Vector2 Process(Vector2 value, InputControl control)
    {
        if (control == null || value == Vector2.zero)
            return value;

        if (!ShouldGate(control))
            return value;

        var clickControl = control.device.TryGetChildControl<ButtonControl>("primary2DAxisClick");
        if (clickControl == null)
            return value;

        return clickControl.isPressed ? value : Vector2.zero;
    }

    private static bool ShouldGate(InputControl control)
    {
        var device = control.device;
        var info = string.Join(" ",
            device.layout ?? string.Empty,
            device.description.interfaceName ?? string.Empty,
            device.description.product ?? string.Empty,
            device.description.manufacturer ?? string.Empty,
            device.displayName ?? string.Empty);

        return info.IndexOf("vive", StringComparison.OrdinalIgnoreCase) >= 0 ||
               info.IndexOf("openvr", StringComparison.OrdinalIgnoreCase) >= 0 ||
               info.IndexOf("steamvr", StringComparison.OrdinalIgnoreCase) >= 0 ||
               info.IndexOf("trackpad", StringComparison.OrdinalIgnoreCase) >= 0 ||
               info.IndexOf("wand", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
