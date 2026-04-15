using UnityEngine;
using UnityEngine.InputSystem;
using TvmVr2.Api.Enums;
using TvmVr2.Client.Sequence;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

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
    private TeleportationProvider teleportationProvider;
    private Sequence sequence;
    private EditingMethodRuntimeSettings methodSettings;
    private bool wasPressed;
    private bool inflateDeflatePickArmed;

    public InteractionMode CurrentMode =>
        inflateDeflatePickArmed ? InteractionMode.InflateDeflatePick : InteractionMode.Teleport;

    void Awake()
    {
        if (leftTeleportation != null)
            rayInteractor = leftTeleportation.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>();

        teleportationProvider = FindFirstObjectByType<TeleportationProvider>();
        sequence = FindFirstObjectByType<Sequence>();
        methodSettings = FindFirstObjectByType<EditingMethodRuntimeSettings>();
    }

    /// <summary>
    /// Activates the ray when the action is triggered
    /// </summary>
    void Update()
    {
        if (leftTeleportation == null || leftActivate.action == null)
            return;

        bool isPressed = leftActivate.action.ReadValue<float>() > 0.01f;
        leftTeleportation.SetActive(isPressed);

        if (wasPressed && !isPressed)
        {
            if (inflateDeflatePickArmed)
                TryPickInflateDeflateReferencePoint();
            else
                TryTeleport();
        }

        wasPressed = isPressed;
    }

    public void BeginInflateDeflatePick()
    {
        if (methodSettings != null)
            methodSettings.CurrentMethod = MethodKind.InflateDeflate;

        inflateDeflatePickArmed = true;
    }

    public void CancelInflateDeflatePick()
    {
        inflateDeflatePickArmed = false;

        if (leftTeleportation != null)
            leftTeleportation.SetActive(false);
    }

    private void TryTeleport()
    {
        if (rayInteractor == null || teleportationProvider == null)
            return;

        if (!rayInteractor.TryGetCurrent3DRaycastHit(out var hit))
            return;

        var anchor = hit.collider.GetComponentInParent<TeleportationAnchor>();
        var area = hit.collider.GetComponentInParent<TeleportationArea>();

        if (anchor == null && area == null)
            return;

        var destination = anchor != null ? anchor.transform.position : hit.point;
        var request = new TeleportRequest
        {
            destinationPosition = destination,
            requestTime = Time.time
        };

        teleportationProvider.QueueTeleportRequest(request);
    }

    private void TryPickInflateDeflateReferencePoint()
    {
        inflateDeflatePickArmed = false;

        if (rayInteractor == null || sequence == null)
            return;

        if (methodSettings != null && methodSettings.CurrentMethod != MethodKind.InflateDeflate)
            return;

        if (!rayInteractor.TryGetCurrent3DRaycastHit(out var hit))
        {
            Debug.LogWarning("InflateDeflate: Reference point was not selected.");
            return;
        }

        if (hit.collider == null)
        {
            Debug.LogWarning("InflateDeflate: Raycast hit has no collider.");
            return;
        }

        var hitSequence = hit.collider.GetComponentInParent<Sequence>();
        if (hitSequence != sequence)
        {
            Debug.LogWarning("InflateDeflate: Aim at the sequence mesh to pick a reference point.");
            return;
        }

        sequence.CommitInflateDeflate(hit.point);
    }
}
