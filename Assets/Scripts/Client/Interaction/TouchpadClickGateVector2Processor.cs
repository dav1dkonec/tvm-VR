using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public sealed class TouchpadClickGateVector2Processor : InputProcessor<Vector2>
{
    private const string ProcessorName = "TouchpadClickGateVector2";
    private static bool registered;

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    private static void RegisterInEditor()
    {
        Register();
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void RegisterAtRuntime()
    {
        Register();
    }

    private static void Register()
    {
        if (registered)
            return;

        InputSystem.RegisterProcessor<TouchpadClickGateVector2Processor>(ProcessorName);
        registered = true;
    }

    public override Vector2 Process(Vector2 value, InputControl control)
    {
        if (control == null || value == Vector2.zero)
            return value;

        if (!ShouldGate(control.device))
            return value;

        var clickControl = control.device.TryGetChildControl<ButtonControl>("primary2DAxisClick");
        if (clickControl == null)
            return value;

        return clickControl.isPressed ? value : Vector2.zero;
    }

    private static bool ShouldGate(InputDevice device)
    {
        if (device == null)
            return false;

        string info = string.Join(" ",
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
