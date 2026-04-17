using UnityEngine;
using UnityEngine.InputSystem;
using TvmVr2.Api.Enums;
using TvmVr2.Client.Sequence;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

/// <summary>
/// Implemented following tutorials by Valem Tutorials
/// Activates the teleportation ray when an action is triggered
/// </summary>
public class ActivateTeleportationRay : MonoBehaviour
{
    /// <summary>
    /// Teleportation ray object
    /// </summary>
    public GameObject leftTeleportation;

    /// <summary>
    /// Activation input action property
    /// </summary>
    public InputActionProperty leftActivate;

    private UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor rayInteractor;
    private Sequence sequence;
    private EditingMethodRuntimeSettings methodSettings;
    private InflateDeflateUI inflateDeflateUi;
    private TeleportationArea[] teleportationAreas;
    private TeleportationAnchor[] teleportationAnchors;
    private XRInteractorLineVisual lineVisual;
    private bool wasPressed;
    private bool inflateDeflatePickArmed;

    public InteractionMode CurrentMode =>
        inflateDeflatePickArmed ? InteractionMode.InflateDeflatePick : InteractionMode.Teleport;

    void Awake()
    {
        if (leftTeleportation != null)
        {
            rayInteractor = leftTeleportation.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>();
            lineVisual = leftTeleportation.GetComponent<XRInteractorLineVisual>();
        }

        sequence = FindFirstObjectByType<Sequence>();
        methodSettings = FindFirstObjectByType<EditingMethodRuntimeSettings>();
        inflateDeflateUi = FindFirstObjectByType<InflateDeflateUI>();
        teleportationAreas = FindObjectsByType<TeleportationArea>(FindObjectsSortMode.None);
        teleportationAnchors = FindObjectsByType<TeleportationAnchor>(FindObjectsSortMode.None);

        // Keep anchor visuals in scene, but remove the ray reticle dot.
        if (lineVisual != null)
            lineVisual.reticle = null;
    }

    /// <summary>
    /// Activates the ray when the action is triggered
    /// </summary>
    void Update()
    {
        if (leftTeleportation == null || leftActivate.action == null)
            return;

        bool isPressed = leftActivate.action.ReadValue<float>() > 0.01f;

        if (wasPressed && !isPressed && inflateDeflatePickArmed)
            TryPickInflateDeflateReferencePoint();

        leftTeleportation.SetActive(isPressed);

        wasPressed = isPressed;
    }

    public void BeginInflateDeflatePick()
    {
        if (methodSettings != null)
            methodSettings.CurrentMethod = MethodKind.InflateDeflate;

        inflateDeflatePickArmed = true;
        SetTeleportTargetsEnabled(false);
    }

    public void CancelInflateDeflatePick()
    {
        inflateDeflatePickArmed = false;
        SetTeleportTargetsEnabled(true);

        if (leftTeleportation != null)
            leftTeleportation.SetActive(false);

        inflateDeflateUi?.ShowPickFailed("Selection cancelled.\nTeleport works normally again.");
    }

    private void TryPickInflateDeflateReferencePoint()
    {
        inflateDeflatePickArmed = false;
        SetTeleportTargetsEnabled(true);

        if (rayInteractor == null || sequence == null)
            return;

        if (methodSettings != null && methodSettings.CurrentMethod != MethodKind.InflateDeflate)
            return;

        if (!TryGetCurrentHit(out var hit))
        {
            inflateDeflateUi?.ShowPickFailed("No valid mesh point was hit.\nPress Pick Point again, use the left hand ray and aim at the sequence mesh.");
            Debug.LogWarning("InflateDeflate: Reference point was not selected.");
            return;
        }

        if (hit.collider == null)
        {
            inflateDeflateUi?.ShowPickFailed("No collider was hit.\nUse the left hand ray and aim at the sequence mesh.");
            Debug.LogWarning("InflateDeflate: Raycast hit has no collider.");
            return;
        }

        var hitSequence = hit.collider.GetComponentInParent<Sequence>();
        if (hitSequence != sequence)
        {
            inflateDeflateUi?.ShowPickFailed("Aim at the loaded sequence mesh with the left hand ray.\nTeleport surfaces cannot be used as reference points.");
            Debug.LogWarning("InflateDeflate: Aim at the sequence mesh to pick a reference point.");
            return;
        }

        inflateDeflateUi?.ShowPickCompleted();
        sequence.CommitInflateDeflate(hit.point);
    }

    private bool TryGetCurrentHit(out RaycastHit hit)
    {
        hit = default;

        if (rayInteractor == null)
            return false;

        if (rayInteractor.TryGetCurrent3DRaycastHit(out hit))
            return true;

        var ray = new Ray(rayInteractor.transform.position, rayInteractor.transform.forward);
        return Physics.Raycast(ray, out hit, 100f);
    }

    private void SetTeleportTargetsEnabled(bool enabled)
    {
        SetTeleportTargetsEnabled(teleportationAreas, enabled);
        SetTeleportTargetsEnabled(teleportationAnchors, enabled);
    }

    private void SetTeleportTargetsEnabled<T>(T[] targets, bool enabled) where T : Component
    {
        if (targets == null)
            return;

        for (int i = 0; i < targets.Length; i++)
        {
            var target = targets[i];
            if (target == null)
                continue;

            target.enabled = enabled;
        }
    }
}
