using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Keeps a hand menu visible after a short grab-button tap until the same tap toggles it off.
/// A longer grab remains available for normal XR select/grab interactions.
/// </summary>
public class StickyHandMenuToggle : MonoBehaviour
{
    public enum MenuHand
    {
        Left,
        Right
    }

    public GameObject menuRoot;
    public InputActionProperty toggleAction;
    public MenuHand hand;
    public float pressThreshold = 0.5f;
    public float maxTapDuration = 0.25f;

    private bool isSticky;
    private bool wasPressed;
    private bool isPendingTap;
    private float pressStartedAt;
    private InflateDeflateUI inflateDeflateUi;

    public static bool SuppressRightGrabToMove { get; private set; }

    public bool IsSticky => isSticky;

    private void OnEnable()
    {
        inflateDeflateUi = FindFirstObjectByType<InflateDeflateUI>();
        toggleAction.action?.Enable();
    }

    private void OnDisable()
    {
        if (hand == MenuHand.Right)
            SuppressRightGrabToMove = false;
    }

    private void Update()
    {
        var action = toggleAction.action;
        if (action == null)
            return;

        bool isPressed = action.ReadValue<float>() >= pressThreshold;

        if (isPressed && !wasPressed)
        {
            if (InflateDeflateUI.IsAnyPickActive)
            {
                inflateDeflateUi?.ShowPickModeBlockedMessage();
                wasPressed = isPressed;
                return;
            }

            isPendingTap = true;
            pressStartedAt = Time.unscaledTime;

            if (hand == MenuHand.Right)
                SuppressRightGrabToMove = true;
        }

        if (isPressed && isPendingTap && Time.unscaledTime - pressStartedAt > maxTapDuration)
        {
            isPendingTap = false;

            if (hand == MenuHand.Right)
                SuppressRightGrabToMove = false;
        }

        if (!isPressed && wasPressed)
        {
            if (isPendingTap)
            {
                isSticky = !isSticky;

                if (isSticky)
                    SetMenuVisible(true);
            }

            isPendingTap = false;

            if (hand == MenuHand.Right)
                SuppressRightGrabToMove = false;
        }

        wasPressed = isPressed;
    }

    private void LateUpdate()
    {
        if (isSticky)
            SetMenuVisible(true);
    }

    private void SetMenuVisible(bool visible)
    {
        if (menuRoot != null && menuRoot.activeSelf != visible)
            menuRoot.SetActive(visible);
    }
}
