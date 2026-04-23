using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

/// <summary>
/// Pins a hand menu near the user after a grab double tap and lets the same grab move it around the user.
/// </summary>
public class PinnedHandMenuController : MonoBehaviour
{
    private const string XrOriginName = "XR Origin";

    public enum MenuHand
    {
        Left,
        Right
    }

    private enum MenuState
    {
        HandAttached,
        Pinned,
        DraggingPinned
    }

    public GameObject menuRoot;
    public Transform handTransform;
    public Transform userRoot;
    public Transform headTransform;
    public InputActionProperty grabAction;
    public InputActionProperty alternateGrabAction;
    public MenuHand hand;
    public float pressThreshold = 0.5f;
    public float tapMaxDuration = 0.4f;
    public float doubleClickWindow = 0.6f;
    public float dragStartHoldTime = 0.15f;
    public float pinnedDistance = 0.7f;
    public float minHeightOffset = -0.45f;
    public float maxHeightOffset = 0.15f;
    public bool faceHead = true;
    public bool pollDirectControllerInput = true;
    public bool pollTriggerAsFallback = true;
    public bool debugLogging;

    private Transform originalParent;
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private bool originalActive;
    private MenuState state;
    private bool wasPressed;
    private bool pressCanBecomeTap;
    private bool pressBlocked;
    private float pressStartedAt;
    private float lastTapAt = -10f;
    private Vector3 pinnedDirectionLocal = Vector3.forward;
    private float pinnedHeightOffset = -0.2f;
    private InflateDeflateUI inflateDeflateUi;

    private static bool rightGrabReserved;

    public static bool SuppressRightGrabToMove => rightGrabReserved;

    private void Awake()
    {
        if (handTransform == null)
            handTransform = transform;

        if (headTransform == null && Camera.main != null)
            headTransform = Camera.main.transform;

        if (userRoot == null && headTransform != null)
            userRoot = ResolveUserRoot(headTransform);

        CacheOriginalMenuTransform();
        inflateDeflateUi = FindFirstObjectByType<InflateDeflateUI>();
    }

    private void OnEnable()
    {
        grabAction.action?.Enable();
        alternateGrabAction.action?.Enable();
        LogDebug($"enabled, grabAction={(grabAction.action != null)}, alternateGrabAction={(alternateGrabAction.action != null)}, directPolling={pollDirectControllerInput}");
    }

    private void OnDisable()
    {
        if (hand == MenuHand.Right)
            rightGrabReserved = false;
    }

    private void Update()
    {
        bool isPressed = IsGrabPressed();

        if (isPressed && !wasPressed)
            BeginPress();

        if (isPressed)
            UpdatePress();

        if (!isPressed && wasPressed)
            EndPress();

        wasPressed = isPressed;
    }

    private void LateUpdate()
    {
        if (state == MenuState.HandAttached)
            return;

        UpdatePinnedTransform();

        if (menuRoot != null && !menuRoot.activeSelf)
            menuRoot.SetActive(true);
    }

    private void BeginPress()
    {
        if (InflateDeflateUI.IsAnyPickActive)
        {
            inflateDeflateUi?.ShowPickModeBlockedMessage();
            pressCanBecomeTap = false;
            pressBlocked = true;
            return;
        }

        pressStartedAt = Time.unscaledTime;
        pressCanBecomeTap = true;
        pressBlocked = false;

        if (hand == MenuHand.Right)
            rightGrabReserved = true;

        LogDebug("press");
    }

    private void UpdatePress()
    {
        if (pressBlocked)
            return;

        float heldFor = Time.unscaledTime - pressStartedAt;

        if (pressCanBecomeTap && heldFor > tapMaxDuration)
            pressCanBecomeTap = false;

        if (state == MenuState.Pinned && heldFor >= dragStartHoldTime)
            state = MenuState.DraggingPinned;

        if (state == MenuState.DraggingPinned)
            UpdatePinnedPlacementFromHand();

        if (hand == MenuHand.Right && state == MenuState.HandAttached && !pressCanBecomeTap)
            rightGrabReserved = false;
    }

    private void EndPress()
    {
        if (pressCanBecomeTap)
            RegisterTap();

        if (state == MenuState.DraggingPinned)
            state = MenuState.Pinned;

        pressCanBecomeTap = false;
        pressBlocked = false;

        if (hand == MenuHand.Right)
            rightGrabReserved = false;
    }

    private void RegisterTap()
    {
        float now = Time.unscaledTime;
        if (now - lastTapAt <= doubleClickWindow)
        {
            LogDebug("double tap");
            TogglePinned();
            lastTapAt = -10f;
            return;
        }

        LogDebug("tap");
        lastTapAt = now;
    }

    private void TogglePinned()
    {
        if (state == MenuState.HandAttached)
            PinMenu();
        else
            UnpinMenu();
    }

    private void PinMenu()
    {
        if (menuRoot == null)
            return;

        CacheOriginalMenuTransform();
        UpdatePinnedPlacementFromHand();

        menuRoot.transform.SetParent(null, true);
        menuRoot.SetActive(true);
        state = MenuState.Pinned;
        UpdatePinnedTransform();
        LogDebug("pinned");
    }

    private void UnpinMenu()
    {
        if (menuRoot == null)
            return;

        menuRoot.transform.SetParent(originalParent, false);
        menuRoot.transform.localPosition = originalLocalPosition;
        menuRoot.transform.localRotation = originalLocalRotation;
        menuRoot.SetActive(originalActive);
        state = MenuState.HandAttached;
        LogDebug("unpinned");
    }

    private bool IsGrabPressed()
    {
        return IsActionPressed(grabAction.action) ||
               IsActionPressed(alternateGrabAction.action) ||
               IsDirectControllerGrabPressed();
    }

    private bool IsActionPressed(InputAction action)
    {
        if (action == null)
            return false;

        try
        {
            if (action.IsPressed())
                return true;

            return action.ReadValue<float>() >= pressThreshold;
        }
        catch
        {
            return false;
        }
    }

    private bool IsDirectControllerGrabPressed()
    {
        if (!pollDirectControllerInput)
            return false;

        XRNode node = hand == MenuHand.Left ? XRNode.LeftHand : XRNode.RightHand;
        InputDevice device = InputDevices.GetDeviceAtXRNode(node);
        if (!device.isValid)
            return false;

        if (device.TryGetFeatureValue(CommonUsages.gripButton, out bool gripButton) && gripButton)
            return true;

        if (device.TryGetFeatureValue(CommonUsages.grip, out float gripValue) && gripValue >= pressThreshold)
            return true;

        if (!pollTriggerAsFallback)
            return false;

        if (device.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerButton) && triggerButton)
            return true;

        return device.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue) && triggerValue >= pressThreshold;
    }

    private void LogDebug(string message)
    {
        if (!debugLogging)
            return;

        Debug.Log($"PinnedHandMenuController[{hand}]: {message}", this);
    }

    private void CacheOriginalMenuTransform()
    {
        if (menuRoot == null)
            return;

        originalParent = menuRoot.transform.parent;
        originalLocalPosition = menuRoot.transform.localPosition;
        originalLocalRotation = menuRoot.transform.localRotation;
        originalActive = menuRoot.activeSelf;
    }

    private void UpdatePinnedPlacementFromHand()
    {
        if (headTransform == null)
            return;

        Vector3 sourcePosition = handTransform != null ? handTransform.position : headTransform.position + GetFallbackWorldDirection();
        Vector3 direction = sourcePosition - headTransform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            direction = GetFallbackWorldDirection();

        direction.Normalize();
        pinnedDirectionLocal = Quaternion.Inverse(GetUserYawRotation()) * direction;
        pinnedDirectionLocal.y = 0f;
        pinnedDirectionLocal.Normalize();
        pinnedHeightOffset = Mathf.Clamp(sourcePosition.y - headTransform.position.y, minHeightOffset, maxHeightOffset);
    }

    private void UpdatePinnedTransform()
    {
        if (menuRoot == null || headTransform == null)
            return;

        Quaternion userYaw = GetUserYawRotation();
        Vector3 direction = userYaw * pinnedDirectionLocal;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            direction = GetFallbackWorldDirection();

        direction.Normalize();
        menuRoot.transform.position = headTransform.position + direction * pinnedDistance + Vector3.up * pinnedHeightOffset;

        if (faceHead)
            FaceHeadYawOnly();
    }

    private void FaceHeadYawOnly()
    {
        Vector3 toHead = headTransform.position - menuRoot.transform.position;
        toHead.y = 0f;

        if (toHead.sqrMagnitude < 0.0001f)
            return;

        menuRoot.transform.rotation = Quaternion.LookRotation(-toHead.normalized, Vector3.up);
    }

    private Quaternion GetUserYawRotation()
    {
        Transform basis = userRoot != null ? userRoot : headTransform;
        return Quaternion.Euler(0f, basis.eulerAngles.y, 0f);
    }

    private Vector3 GetFallbackWorldDirection()
    {
        Vector3 side = headTransform != null ? headTransform.right : transform.right;
        side.y = 0f;

        if (side.sqrMagnitude < 0.0001f)
            side = Vector3.right;

        side.Normalize();
        return hand == MenuHand.Left ? -side : side;
    }

    private static Transform ResolveUserRoot(Transform from)
    {
        Transform current = from;
        while (current != null)
        {
            if (current.name == XrOriginName)
                return current;

            current = current.parent;
        }

        return from.root;
    }
}
