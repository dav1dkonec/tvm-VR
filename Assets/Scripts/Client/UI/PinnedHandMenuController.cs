using System;
using UnityEngine;

/// <summary>
/// Pins a hand menu near the user after a grab double tap and lets the same grab move it around the user.
/// </summary>
public class PinnedHandMenuController : MonoBehaviour
{
    /// <summary>
    /// Hand that owns the pinned menu.
    /// </summary>
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

    /// <summary>
    /// Scene references required for menu pinning and orientation.
    /// </summary>
    [System.Serializable]
    public struct HandMenuBindings
    {
        /// <summary>
        /// Root object of the menu.
        /// </summary>
        public GameObject menuRoot;

        /// <summary>
        /// Canvas used as the readable face of the menu.
        /// </summary>
        public Canvas visualCanvas;

        /// <summary>
        /// Transform of the controller hand.
        /// </summary>
        public Transform handTransform;

        /// <summary>
        /// User rig transform used as yaw reference.
        /// </summary>
        public Transform userRoot;

        /// <summary>
        /// Head transform used as placement and facing reference.
        /// </summary>
        public Transform headTransform;

        /// <summary>
        /// Inflate/deflate panel controlled by this hand menu.
        /// </summary>
        public InflateDeflateUI inflateDeflateUi;
    }

    [SerializeField] private HandMenuBindings bindings;
    /// <summary>
    /// Hand used for controller input and side-specific facing.
    /// </summary>
    public MenuHand hand;

    /// <summary>
    /// Analog grip threshold treated as pressed.
    /// </summary>
    public float pressThreshold = 0.5f;

    /// <summary>
    /// Maximum press duration accepted as a tap.
    /// </summary>
    public float tapMaxDuration = 0.4f;

    /// <summary>
    /// Time window in which two taps pin or unpin the menu.
    /// </summary>
    public float doubleClickWindow = 0.6f;

    /// <summary>
    /// Hold duration after which a pinned menu starts dragging.
    /// </summary>
    public float dragStartHoldTime = 0.15f;

    /// <summary>
    /// Distance of the pinned menu from the user's head.
    /// </summary>
    public float orbitRadius = 0.55f;

    /// <summary>
    /// Horizontal drag sensitivity expressed as orbit degrees per meter.
    /// </summary>
    public float dragDegreesPerMeter = 80f;

    /// <summary>
    /// Vertical drag sensitivity for menu height.
    /// </summary>
    public float dragVerticalSensitivity = 1f;

    /// <summary>
    /// Lowest allowed pinned height relative to the head.
    /// </summary>
    public float minHeightOffset = -0.33f;

    /// <summary>
    /// Highest allowed pinned height relative to the head.
    /// </summary>
    public float maxHeightOffset = 0.15f;

    /// <summary>
    /// Initial pinned height relative to the head.
    /// </summary>
    public float defaultPinnedHeightOffset = 0f;

    /// <summary>
    /// Manual pitch offset for pinned menu orientation.
    /// </summary>
    public float pinnedPitchDegrees = 0f;

    /// <summary>
    /// Side-dependent yaw offset for pinned menu orientation.
    /// </summary>
    public float pinnedYawDegrees = 6f;

    /// <summary>
    /// Scales automatic pitch toward the user's head.
    /// </summary>
    public float verticalFacingSensitivity = 1.35f;

    /// <summary>
    /// Maximum automatic upward pitch.
    /// </summary>
    public float maxUpwardAutoPitchDegrees = 14f;

    /// <summary>
    /// Maximum automatic downward pitch.
    /// </summary>
    public float maxDownwardAutoPitchDegrees = 18f;

    /// <summary>
    /// Whether the canvas readable side faces opposite to its forward direction.
    /// </summary>
    public bool invertCanvasFacing = true;

    /// <summary>
    /// Extra rotation applied after facing the menu toward the user.
    /// </summary>
    public Vector3 pinnedAdditionalRotationEuler;

    /// <summary>
    /// Whether this component polls XR controller grip input directly.
    /// </summary>
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

    /// <summary>
    /// Whether right-hand grab-to-move should ignore input currently consumed by menu pinning.
    /// </summary>
    public static bool SuppressRightGrabToMove => rightGrabReserved;

    private void Awake()
    {
        CacheOriginalMenuTransform();
        CacheVisibilityComponents();

        if (VisualFaceTransform != null && VisualFaceTransform != bindings.menuRoot.transform)
            baseVisualLocalRotation = VisualFaceTransform.localRotation;
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

    /// <summary>
    /// Starts dragging a pinned menu and stores the initial hand/menu relationship.
    /// </summary>
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

    /// <summary>
    /// Pins the hand-attached menu in world space or restores it to the hand.
    /// </summary>
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
            heightOffset = Mathf.Clamp(defaultPinnedHeightOffset, minHeightOffset, maxHeightOffset);
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

    /// <summary>
    /// Resolves current grab state while center selection has priority over menu pinning.
    /// </summary>
    private bool IsGrabPressed()
    {
        if (CenterUI.HasActiveSelection)
            return false;

        if (TryGetDirectControllerGrabState(out bool directPressed))
            return directPressed;

        return false;
    }

    /// <summary>
    /// Reads grip button or analog grip value from the configured XR controller.
    /// </summary>
    private bool TryGetDirectControllerGrabState(out bool isPressed)
    {
        isPressed = false;

        if (!pollDirectControllerInput)
            return false;

        UnityEngine.XR.XRNode node = hand == MenuHand.Left ? UnityEngine.XR.XRNode.LeftHand : UnityEngine.XR.XRNode.RightHand;
        UnityEngine.XR.InputDevice device = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(node);
        if (!device.isValid)
            return false;

        if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out bool gripButton) && gripButton)
        {
            isPressed = true;
            return true;
        }

        if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.grip, out float gripValue) && gripValue >= pressThreshold)
        {
            isPressed = true;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Stores the hand-attached transform so unpinning can restore the menu exactly.
    /// </summary>
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

    /// <summary>
    /// Caches canvases and canvas groups that must stay visible while pinned.
    /// </summary>
    private void CacheVisibilityComponents()
    {
        GameObject menuRoot = bindings.menuRoot;
        if (menuRoot == null)
            return;

        visibilityCanvases = menuRoot.GetComponentsInChildren<Canvas>(true);
        visibilityCanvasGroups = menuRoot.GetComponentsInChildren<CanvasGroup>(true);
    }

    /// <summary>
    /// Converts controller drag movement into orbit angle and vertical offset changes.
    /// </summary>
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

    /// <summary>
    /// Initializes orbit angle and height from the menu's current world position.
    /// </summary>
    private void CaptureOrbitFromWorldPosition(Vector3 worldPosition)
    {
        Transform headTransform = bindings.headTransform;
        if (headTransform == null)
            return;

        Vector3 localDirection = Quaternion.Inverse(GetYawRotation(bindings.userRoot != null ? bindings.userRoot : headTransform)) * GetHorizontalDirection(worldPosition - headTransform.position);
        orbitAngleDegrees = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;
        heightOffset = Mathf.Clamp(worldPosition.y - headTransform.position.y, minHeightOffset, maxHeightOffset);
    }

    /// <summary>
    /// Places the pinned menu at the current orbit angle and height relative to the head.
    /// </summary>
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

    /// <summary>
    /// Rotates the pinned menu so its visual canvas stays readable from the head position.
    /// </summary>
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
        autoPitchDegrees = autoPitchDegrees >= 0f
            ? Mathf.Min(autoPitchDegrees, maxUpwardAutoPitchDegrees)
            : Mathf.Max(autoPitchDegrees, -maxDownwardAutoPitchDegrees);
        float totalPitchDegrees = autoPitchDegrees;
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

    /// <summary>
    /// Re-enables menu canvases after other UI actions hide or disable them.
    /// </summary>
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

    /// <summary>
    /// Hides a pinned menu while the application is busy.
    /// </summary>
    public void SuspendForBusy()
    {
        if (state == MenuState.HandAttached || bindings.menuRoot == null)
            return;

        bindings.menuRoot.SetActive(false);
    }

    /// <summary>
    /// Restores a pinned menu after the busy state ends.
    /// </summary>
    public void ResumeAfterBusy()
    {
        if (state == MenuState.HandAttached || bindings.menuRoot == null)
            return;

        bindings.menuRoot.SetActive(true);
        UpdatePinnedTransform();
        EnsurePinnedMenuVisible();
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
