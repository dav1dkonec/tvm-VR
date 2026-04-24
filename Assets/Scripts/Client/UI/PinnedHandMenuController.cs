using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Pins a hand menu near the user after a grab double tap and lets the same grab move it around the user.
/// </summary>
public class PinnedHandMenuController : MonoBehaviour
{
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

    [System.Serializable]
    public struct HandMenuBindings
    {
        public GameObject menuRoot;
        public Canvas visualCanvas;
        public Transform handTransform;
        public Transform userRoot;
        public Transform headTransform;
        public InflateDeflateUI inflateDeflateUi;
        public InputActionProperty grabAction;
        public InputActionProperty alternateGrabAction;
    }

    [SerializeField] private HandMenuBindings bindings;
    public MenuHand hand;
    public float pressThreshold = 0.5f;
    public float tapMaxDuration = 0.4f;
    public float doubleClickWindow = 0.6f;
    public float dragStartHoldTime = 0.15f;
    public float orbitRadius = 0.55f;
    public float dragDegreesPerMeter = 80f;
    public float dragVerticalSensitivity = 1f;
    public float minHeightOffset = -0.45f;
    public float maxHeightOffset = 0.15f;
    public float pinnedPitchDegrees = -4f;
    public float pinnedYawDegrees = 6f;
    public float verticalFacingSensitivity = 1.35f;
    public float maxAutoPitchDegrees = 14f;
    public bool invertCanvasFacing = true;
    public Vector3 pinnedAdditionalRotationEuler;
    public bool pollDirectControllerInput = true;

    private Transform HandTransform => bindings.handTransform != null ? bindings.handTransform : transform;
    private Transform VisualFaceTransform => bindings.visualCanvas != null ? bindings.visualCanvas.transform : bindings.menuRoot != null ? bindings.menuRoot.transform : null;

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
    private Vector3 dragStartHandPosition;
    private Quaternion dragBasisYawRotation = Quaternion.identity;
    private float orbitAngleDegrees;
    private float dragStartHeightOffset;
    private float dragStartOrbitAngleDegrees;
    private float heightOffset = -0.2f;
    private Quaternion baseVisualLocalRotation = Quaternion.identity;
    private Canvas[] visibilityCanvases = Array.Empty<Canvas>();
    private CanvasGroup[] visibilityCanvasGroups = Array.Empty<CanvasGroup>();

    private static bool rightGrabReserved;

    public static bool SuppressRightGrabToMove => rightGrabReserved;

    private void Awake()
    {
        CacheOriginalMenuTransform();
        CacheVisibilityComponents();

        if (VisualFaceTransform != null && VisualFaceTransform != bindings.menuRoot.transform)
            baseVisualLocalRotation = VisualFaceTransform.localRotation;
    }

    private void OnEnable()
    {
        bindings.grabAction.action?.Enable();
        bindings.alternateGrabAction.action?.Enable();
    }

    private void OnDisable()
    {
        if (hand == MenuHand.Right)
            rightGrabReserved = false;
    }

    private void Update()
    {
        bool isPressed = IsGrabPressed();
        HandlePressState(isPressed, Time.unscaledTime);
        wasPressed = isPressed;
    }

    private void LateUpdate()
    {
        if (state == MenuState.HandAttached)
            return;

        UpdatePinnedTransform();
        EnsurePinnedMenuVisible();
    }

    private void HandlePressState(bool isPressed, float now)
    {
        if (isPressed && !wasPressed)
        {
            if (InflateDeflateUI.IsAnyPickActive)
            {
                bindings.inflateDeflateUi?.ShowPickModeBlockedMessage();
                pressCanBecomeTap = false;
                pressBlocked = true;
                return;
            }

            pressStartedAt = now;
            pressCanBecomeTap = true;
            pressBlocked = false;

            if (hand == MenuHand.Right)
                rightGrabReserved = true;
        }

        if (!isPressed)
        {
            if (!wasPressed)
                return;

            if (pressCanBecomeTap)
            {
                if (now - lastTapAt <= doubleClickWindow)
                {
                    SetPinned(state == MenuState.HandAttached);
                    lastTapAt = -10f;
                }
                else
                {
                    lastTapAt = now;
                }
            }

            if (state == MenuState.DraggingPinned)
                state = MenuState.Pinned;

            pressCanBecomeTap = false;
            pressBlocked = false;

            if (hand == MenuHand.Right)
                rightGrabReserved = false;

            return;
        }

        if (pressBlocked)
            return;

        float heldFor = now - pressStartedAt;

        if (pressCanBecomeTap && heldFor > tapMaxDuration)
            pressCanBecomeTap = false;

        if (state == MenuState.Pinned && heldFor >= dragStartHoldTime)
            BeginPinnedDrag();

        if (state == MenuState.DraggingPinned)
            UpdatePinnedPlacementFromDrag();

        if (hand == MenuHand.Right && state == MenuState.HandAttached && !pressCanBecomeTap)
            rightGrabReserved = false;
    }

    private void BeginPinnedDrag()
    {
        state = MenuState.DraggingPinned;
        pressCanBecomeTap = false;

        Transform handTransform = HandTransform;
        dragStartHandPosition = handTransform != null ? handTransform.position : bindings.menuRoot.transform.position;
        dragBasisYawRotation = GetYawRotation(bindings.headTransform != null ? bindings.headTransform : bindings.userRoot);
        dragStartOrbitAngleDegrees = orbitAngleDegrees;
        dragStartHeightOffset = heightOffset;
    }

    private void SetPinned(bool pinned)
    {
        GameObject menuRoot = bindings.menuRoot;
        if (menuRoot == null)
            return;

        if (pinned)
        {
            Transform visualFaceTransform = VisualFaceTransform;
            if (visualFaceTransform == null)
                return;

            CacheOriginalMenuTransform();
            CaptureOrbitFromWorldPosition(menuRoot.transform.position);
            menuRoot.transform.SetParent(null, true);
            menuRoot.SetActive(true);
            state = MenuState.Pinned;
            UpdatePinnedTransform();
            return;
        }

        RestoreVisualLocalRotation();
        menuRoot.transform.SetParent(originalParent, false);
        menuRoot.transform.localPosition = originalLocalPosition;
        menuRoot.transform.localRotation = originalLocalRotation;
        menuRoot.SetActive(originalActive);
        state = MenuState.HandAttached;
    }

    private bool IsGrabPressed()
    {
        return IsActionPressed(bindings.grabAction.action) ||
               IsActionPressed(bindings.alternateGrabAction.action) ||
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

        UnityEngine.XR.XRNode node = hand == MenuHand.Left ? UnityEngine.XR.XRNode.LeftHand : UnityEngine.XR.XRNode.RightHand;
        UnityEngine.XR.InputDevice device = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(node);
        if (!device.isValid)
            return false;

        if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out bool gripButton) && gripButton)
            return true;

        if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.grip, out float gripValue) && gripValue >= pressThreshold)
            return true;

        if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out bool triggerButton) && triggerButton)
            return true;

        return device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out float triggerValue) && triggerValue >= pressThreshold;
    }

    private void CacheOriginalMenuTransform()
    {
        GameObject menuRoot = bindings.menuRoot;
        if (menuRoot == null)
            return;

        originalParent = menuRoot.transform.parent;
        originalLocalPosition = menuRoot.transform.localPosition;
        originalLocalRotation = menuRoot.transform.localRotation;
        originalActive = menuRoot.activeSelf;
    }

    private void CacheVisibilityComponents()
    {
        GameObject menuRoot = bindings.menuRoot;
        if (menuRoot == null)
            return;

        visibilityCanvases = menuRoot.GetComponentsInChildren<Canvas>(true);
        visibilityCanvasGroups = menuRoot.GetComponentsInChildren<CanvasGroup>(true);
    }

    private void UpdatePinnedPlacementFromDrag()
    {
        Transform handTransform = HandTransform;
        if (handTransform == null || bindings.headTransform == null)
            return;

        Vector3 localDelta = Quaternion.Inverse(dragBasisYawRotation) * (handTransform.position - dragStartHandPosition);

        orbitAngleDegrees = dragStartOrbitAngleDegrees + localDelta.x * dragDegreesPerMeter;
        heightOffset = Mathf.Clamp(
            dragStartHeightOffset + localDelta.y * dragVerticalSensitivity,
            minHeightOffset,
            maxHeightOffset);
    }

    private void CaptureOrbitFromWorldPosition(Vector3 worldPosition)
    {
        Transform headTransform = bindings.headTransform;
        if (headTransform == null)
            return;

        Vector3 localDirection = Quaternion.Inverse(GetYawRotation(bindings.userRoot != null ? bindings.userRoot : headTransform)) * GetHorizontalDirection(worldPosition - headTransform.position);
        orbitAngleDegrees = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;
        heightOffset = Mathf.Clamp(worldPosition.y - headTransform.position.y, minHeightOffset, maxHeightOffset);
    }

    private void UpdatePinnedTransform()
    {
        GameObject menuRoot = bindings.menuRoot;
        Transform headTransform = bindings.headTransform;
        if (menuRoot == null || headTransform == null)
            return;

        Quaternion userYaw = GetYawRotation(bindings.userRoot != null ? bindings.userRoot : headTransform);
        Vector3 localDirection = Quaternion.Euler(0f, orbitAngleDegrees, 0f) * Vector3.forward;
        Vector3 worldDirection = userYaw * localDirection;
        worldDirection.y = 0f;
        worldDirection.Normalize();
        menuRoot.transform.position = headTransform.position + worldDirection * orbitRadius + Vector3.up * heightOffset;
        RotatePinnedMenuTowardHead();
    }

    private void RotatePinnedMenuTowardHead()
    {
        GameObject menuRoot = bindings.menuRoot;
        Transform headTransform = bindings.headTransform;
        if (menuRoot == null || headTransform == null)
            return;

        Transform visualFaceTransform = VisualFaceTransform;
        if (visualFaceTransform == null)
            return;

        Vector3 toUser = bindings.headTransform.position - visualFaceTransform.position;
        if (toUser.sqrMagnitude < 0.0001f)
            return;

        Vector3 toUserHorizontal = toUser;
        toUserHorizontal.y = 0f;
        if (toUserHorizontal.sqrMagnitude < 0.0001f)
            toUserHorizontal = GetFallbackWorldDirection();
        else
            toUserHorizontal.Normalize();

        Vector3 visualForward = invertCanvasFacing ? -toUserHorizontal : toUserHorizontal;
        Quaternion faceRotation = Quaternion.LookRotation(visualForward, Vector3.up);
        float autoPitchDegrees = Mathf.Atan2(toUser.y * verticalFacingSensitivity, Mathf.Max(0.001f, new Vector2(toUser.x, toUser.z).magnitude)) * Mathf.Rad2Deg;
        autoPitchDegrees = Mathf.Clamp(autoPitchDegrees, -maxAutoPitchDegrees, maxAutoPitchDegrees);
        float totalPitchDegrees = pinnedPitchDegrees + autoPitchDegrees;
        float handYaw = hand == MenuHand.Left ? pinnedYawDegrees : -pinnedYawDegrees;
        Quaternion visualTilt = Quaternion.Euler(totalPitchDegrees, handYaw, 0f);

        if (visualFaceTransform != menuRoot.transform)
        {
            visualFaceTransform.localRotation = baseVisualLocalRotation * visualTilt;
            menuRoot.transform.rotation = faceRotation * Quaternion.Inverse(baseVisualLocalRotation) * Quaternion.Euler(pinnedAdditionalRotationEuler);
            return;
        }

        menuRoot.transform.rotation = faceRotation * visualTilt * Quaternion.Euler(pinnedAdditionalRotationEuler);
    }

    private void EnsurePinnedMenuVisible()
    {
        GameObject menuRoot = bindings.menuRoot;
        if (menuRoot == null)
            return;

        if (!menuRoot.activeSelf)
            menuRoot.SetActive(true);

        Transform visualFaceTransform = VisualFaceTransform;
        if (visualFaceTransform != null && !visualFaceTransform.gameObject.activeSelf)
            visualFaceTransform.gameObject.SetActive(true);

        foreach (Canvas canvas in visibilityCanvases)
        {
            if (!canvas.gameObject.activeSelf)
                canvas.gameObject.SetActive(true);

            canvas.enabled = true;
        }

        foreach (CanvasGroup canvasGroup in visibilityCanvasGroups)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }

    private Vector3 GetHorizontalDirection(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return GetFallbackWorldDirection();

        direction.Normalize();
        return direction;
    }

    private static Quaternion GetYawRotation(Transform basis)
    {
        return basis != null ? Quaternion.Euler(0f, basis.eulerAngles.y, 0f) : Quaternion.identity;
    }

    private Vector3 GetFallbackWorldDirection()
    {
        Transform headTransform = bindings.headTransform;
        Vector3 side = headTransform != null ? headTransform.right : transform.right;
        side.y = 0f;

        if (side.sqrMagnitude < 0.0001f)
            side = Vector3.right;

        side.Normalize();
        return hand == MenuHand.Left ? -side : side;
    }

    private void RestoreVisualLocalRotation()
    {
        Transform visualFaceTransform = VisualFaceTransform;
        if (visualFaceTransform != null && visualFaceTransform != bindings.menuRoot.transform)
            visualFaceTransform.localRotation = baseVisualLocalRotation;
    }

}
