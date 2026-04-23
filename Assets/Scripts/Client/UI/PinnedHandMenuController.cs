using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Pins a hand menu near the user after a grab double tap and lets the same grab move it around the user.
/// </summary>
public class PinnedHandMenuController : MonoBehaviour
{
    private const string XrOriginName = "XR Origin";
    private const string LeftHandName = "Left Hand";
    private const string RightHandName = "Right Hand";
    private const string MethodMenuName = "Method Menu";
    private const string PlaybackMenuName = "Playback Menu";

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
    public float orbitRadius = 0.55f;
    public float dragDegreesPerMeter = 180f;
    public float dragVerticalSensitivity = 1f;
    public float minHeightOffset = -0.45f;
    public float maxHeightOffset = 0.15f;
    public bool faceHead = true;
    public bool useFixedPinnedPitch = true;
    public float pinnedPitchDegrees = -6f;
    public float pinnedYawDegrees = 6f;
    public bool invertCanvasFacing = true;
    public Vector3 pinnedAdditionalRotationEuler;
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
    private Vector3 dragStartHandPosition;
    private Quaternion dragBasisYawRotation = Quaternion.identity;
    private float orbitAngleDegrees;
    private float dragStartHeightOffset;
    private float dragStartOrbitAngleDegrees;
    private float heightOffset = -0.2f;
    private Transform visualFaceTransform;
    private Quaternion visualLocalRotation = Quaternion.identity;
    private InflateDeflateUI inflateDeflateUi;

    private static bool rightGrabReserved;

    public static bool SuppressRightGrabToMove => rightGrabReserved;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void LogPinnedControllersAfterSceneLoad()
    {
        var controllers = FindObjectsByType<PinnedHandMenuController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Debug.Log($"PinnedHandMenuController diagnostic: scene='{SceneManager.GetActiveScene().name}', controllers={controllers.Length}");

        if (controllers.Length == 0)
        {
            InstallRuntimeFallbackControllers();
            controllers = FindObjectsByType<PinnedHandMenuController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Debug.Log($"PinnedHandMenuController diagnostic: after runtime fallback install, controllers={controllers.Length}");
        }

        foreach (var controller in controllers)
        {
            Debug.Log(
                $"PinnedHandMenuController diagnostic: hand={controller.hand}, object='{controller.name}', activeSelf={controller.gameObject.activeSelf}, activeInHierarchy={controller.gameObject.activeInHierarchy}, enabled={controller.enabled}, menuRoot='{controller.menuRoot?.name}', grabAction={(controller.grabAction.action != null)}, alternateGrabAction={(controller.alternateGrabAction.action != null)}",
                controller);
        }
    }

    private static void InstallRuntimeFallbackControllers()
    {
        InstallRuntimeFallbackController(LeftHandName, MethodMenuName, MenuHand.Left);
        InstallRuntimeFallbackController(RightHandName, PlaybackMenuName, MenuHand.Right);
    }

    private static void InstallRuntimeFallbackController(string handName, string menuName, MenuHand menuHand)
    {
        Transform handObject = FindSceneTransformByName(handName);
        Transform menuObject = FindSceneTransformByName(menuName);

        if (handObject == null || menuObject == null)
        {
            Debug.LogWarning($"PinnedHandMenuController diagnostic: cannot install {menuHand} fallback, hand='{handObject?.name}', menu='{menuObject?.name}'");
            return;
        }

        var controller = handObject.GetComponent<PinnedHandMenuController>();
        if (controller == null)
            controller = handObject.gameObject.AddComponent<PinnedHandMenuController>();

        controller.ConfigureRuntimeFallback(menuObject.gameObject, menuHand);
        Debug.Log($"PinnedHandMenuController diagnostic: installed {menuHand} fallback on '{handObject.name}' for menu '{menuObject.name}'", controller);
    }

    private static Transform FindSceneTransformByName(string objectName)
    {
        foreach (var candidate in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (candidate.name == objectName && candidate.gameObject.scene.IsValid())
                return candidate;
        }

        return null;
    }

    private void Awake()
    {
        ResolveMissingReferences();
        inflateDeflateUi = FindFirstObjectByType<InflateDeflateUI>();
    }

    private void OnEnable()
    {
        grabAction.action?.Enable();
        alternateGrabAction.action?.Enable();
        Debug.Log($"PinnedHandMenuController[{hand}]: enabled, object='{name}', grabAction={(grabAction.action != null)}, alternateGrabAction={(alternateGrabAction.action != null)}, directPolling={pollDirectControllerInput}", this);
    }

    private void OnDisable()
    {
        if (hand == MenuHand.Right)
            rightGrabReserved = false;
    }

    private void ConfigureRuntimeFallback(GameObject runtimeMenuRoot, MenuHand runtimeHand)
    {
        menuRoot = runtimeMenuRoot;
        hand = runtimeHand;
        handTransform = transform;
        visualFaceTransform = null;
        orbitRadius = 0.55f;
        pinnedPitchDegrees = -6f;
        pinnedYawDegrees = 6f;
        invertCanvasFacing = true;
        dragDegreesPerMeter = runtimeHand == MenuHand.Left ? 360f : 180f;
        pinnedAdditionalRotationEuler = Vector3.zero;
        debugLogging = true;
        pollDirectControllerInput = true;
        pollTriggerAsFallback = true;
        ResolveMissingReferences();
        CacheOriginalMenuTransform();
    }

    private void ResolveMissingReferences()
    {
        if (handTransform == null)
            handTransform = transform;

        if (headTransform == null && Camera.main != null)
            headTransform = Camera.main.transform;

        if (userRoot == null)
        {
            Transform xrOrigin = FindSceneTransformByName(XrOriginName);
            if (xrOrigin != null)
                userRoot = xrOrigin;
            else if (headTransform != null)
                userRoot = ResolveUserRoot(headTransform);
        }

        CacheOriginalMenuTransform();
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
        EnsurePinnedMenuVisible();
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
            BeginPinnedDrag();

        if (state == MenuState.DraggingPinned)
            UpdatePinnedPlacementFromDrag();

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

    private void BeginPinnedDrag()
    {
        state = MenuState.DraggingPinned;
        pressCanBecomeTap = false;

        dragStartHandPosition = handTransform != null ? handTransform.position : menuRoot.transform.position;
        dragBasisYawRotation = GetHeadYawRotation();
        dragStartOrbitAngleDegrees = orbitAngleDegrees;
        dragStartHeightOffset = heightOffset;
        LogDebug("drag start");
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
        CaptureOrbitFromMenu();
        CaptureVisualFacingReference();

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

        UnityEngine.XR.XRNode node = hand == MenuHand.Left ? UnityEngine.XR.XRNode.LeftHand : UnityEngine.XR.XRNode.RightHand;
        UnityEngine.XR.InputDevice device = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(node);
        if (!device.isValid)
            return false;

        if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out bool gripButton) && gripButton)
            return true;

        if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.grip, out float gripValue) && gripValue >= pressThreshold)
            return true;

        if (!pollTriggerAsFallback)
            return false;

        if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out bool triggerButton) && triggerButton)
            return true;

        return device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out float triggerValue) && triggerValue >= pressThreshold;
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

    private void UpdatePinnedPlacementFromDrag()
    {
        if (handTransform == null || headTransform == null)
            return;

        Vector3 localDelta = Quaternion.Inverse(dragBasisYawRotation) * (handTransform.position - dragStartHandPosition);

        orbitAngleDegrees = dragStartOrbitAngleDegrees + localDelta.x * dragDegreesPerMeter;
        heightOffset = Mathf.Clamp(
            dragStartHeightOffset + localDelta.y * dragVerticalSensitivity,
            minHeightOffset,
            maxHeightOffset);
    }

    private void CaptureOrbitFromMenu()
    {
        if (menuRoot == null)
            return;

        CaptureOrbitFromWorldPosition(menuRoot.transform.position);
    }

    private void CaptureOrbitFromWorldPosition(Vector3 worldPosition)
    {
        if (headTransform == null)
            return;

        Vector3 localDirection = Quaternion.Inverse(GetUserYawRotation()) * GetHorizontalDirection(worldPosition - headTransform.position);
        orbitAngleDegrees = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;
        heightOffset = Mathf.Clamp(worldPosition.y - headTransform.position.y, minHeightOffset, maxHeightOffset);
    }

    private void UpdatePinnedTransform()
    {
        if (menuRoot == null || headTransform == null)
            return;

        Quaternion userYaw = GetUserYawRotation();
        Vector3 localDirection = Quaternion.Euler(0f, orbitAngleDegrees, 0f) * Vector3.forward;
        Vector3 worldDirection = userYaw * localDirection;
        worldDirection.y = 0f;
        worldDirection.Normalize();
        menuRoot.transform.position = headTransform.position + worldDirection * orbitRadius + Vector3.up * heightOffset;

        if (faceHead)
            RotatePinnedMenuTowardHead(worldDirection);
        else
            menuRoot.transform.rotation = userYaw * Quaternion.Euler(pinnedAdditionalRotationEuler);
    }

    private void RotatePinnedMenuTowardHead(Vector3 worldDirectionFromUser)
    {
        if (menuRoot == null || headTransform == null)
            return;

        Vector3 toUser = -worldDirectionFromUser;
        toUser.y = 0f;

        if (toUser.sqrMagnitude < 0.0001f)
            return;

        toUser.Normalize();
        ResolveVisualFaceTransform();
        Vector3 visualForward = invertCanvasFacing ? -toUser : toUser;
        Quaternion desiredVisualRotation = Quaternion.LookRotation(visualForward, Vector3.up);
        float handYaw = hand == MenuHand.Left ? pinnedYawDegrees : -pinnedYawDegrees;

        if (useFixedPinnedPitch)
            desiredVisualRotation *= Quaternion.Euler(pinnedPitchDegrees, handYaw, 0f);
        else
            desiredVisualRotation *= Quaternion.Euler(0f, handYaw, 0f);

        menuRoot.transform.rotation = desiredVisualRotation * Quaternion.Inverse(visualLocalRotation) * Quaternion.Euler(pinnedAdditionalRotationEuler);
    }

    private void EnsurePinnedMenuVisible()
    {
        if (menuRoot == null)
            return;

        if (!menuRoot.activeSelf)
            menuRoot.SetActive(true);

        ResolveVisualFaceTransform();

        if (visualFaceTransform != null && !visualFaceTransform.gameObject.activeSelf)
            visualFaceTransform.gameObject.SetActive(true);

        foreach (Canvas canvas in menuRoot.GetComponentsInChildren<Canvas>(true))
        {
            if (!canvas.gameObject.activeSelf)
                canvas.gameObject.SetActive(true);

            canvas.enabled = true;
        }

        foreach (CanvasGroup canvasGroup in menuRoot.GetComponentsInChildren<CanvasGroup>(true))
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }

    private void CaptureVisualFacingReference()
    {
        ResolveVisualFaceTransform();

        if (menuRoot == null || visualFaceTransform == null)
            return;

        visualLocalRotation = Quaternion.Inverse(menuRoot.transform.rotation) * visualFaceTransform.rotation;

        if (headTransform == null)
            return;

        LogDebug($"visual face='{visualFaceTransform.name}'");
    }

    private void ResolveVisualFaceTransform()
    {
        if (visualFaceTransform != null || menuRoot == null)
            return;

        Canvas canvas = menuRoot.GetComponentInChildren<Canvas>(true);
        visualFaceTransform = canvas != null ? canvas.transform : menuRoot.transform;
    }

    private Vector3 GetHorizontalDirection(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return GetFallbackWorldDirection();

        direction.Normalize();
        return direction;
    }

    private Quaternion GetUserYawRotation()
    {
        Transform basis = userRoot != null ? userRoot : headTransform;
        return Quaternion.Euler(0f, basis.eulerAngles.y, 0f);
    }

    private Quaternion GetHeadYawRotation()
    {
        Transform basis = headTransform != null ? headTransform : userRoot;
        return basis != null ? Quaternion.Euler(0f, basis.eulerAngles.y, 0f) : Quaternion.identity;
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
