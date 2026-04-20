using UnityEngine;
using UnityEngine.InputSystem;
using TvmVr2.Api.Enums;
using TvmVr2.Client.Centers;
using TvmVr2.Client.Sequence;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

/// <summary>
/// Implemented following tutorials by Valem Tutorials
/// Activates the teleportation ray when an action is triggered
/// </summary>
public class ActivateTeleportationRay : MonoBehaviour
{
    private const float AnchorSnapRadius = 1.5f;

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
    private CenterPool centerPool;
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
        centerPool = FindFirstObjectByType<CenterPool>();
        methodSettings = FindFirstObjectByType<EditingMethodRuntimeSettings>();
        inflateDeflateUi = FindFirstObjectByType<InflateDeflateUI>();
        teleportationAreas = FindObjectsByType<TeleportationArea>(FindObjectsSortMode.None);
        teleportationAnchors = FindObjectsByType<TeleportationAnchor>(FindObjectsSortMode.None);

        HideTeleportRayReticle();
        HideTeleportAreaVisuals();
        ExpandTeleportAnchorTolerance();
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
        else if (isPressed && inflateDeflatePickArmed)
            UpdateInflateDeflatePreview();

        leftTeleportation.SetActive(isPressed);

        wasPressed = isPressed;
    }

    public void BeginInflateDeflatePick()
    {
        if (methodSettings != null)
            methodSettings.CurrentMethod = MethodKind.InflateDeflate;

        inflateDeflatePickArmed = true;
        centerPool?.ClearPreview();
        SetTeleportTargetsEnabled(false);
    }

    public void CancelInflateDeflatePick()
    {
        inflateDeflatePickArmed = false;
        centerPool?.ClearPreview();
        SetTeleportTargetsEnabled(true);

        if (leftTeleportation != null)
            leftTeleportation.SetActive(false);

        inflateDeflateUi?.ShowPickFailed("Selection cancelled.\nTeleport works normally again.");
    }

    private void TryPickInflateDeflateReferencePoint()
    {
        inflateDeflatePickArmed = false;
        centerPool?.ClearPreview();
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

    private void UpdateInflateDeflatePreview()
    {
        if (rayInteractor == null || sequence == null)
            return;

        if (methodSettings != null && methodSettings.CurrentMethod != MethodKind.InflateDeflate)
        {
            centerPool?.ClearPreview();
            return;
        }

        if (!TryGetCurrentHit(out var hit) || hit.collider == null)
        {
            centerPool?.ClearPreview();
            return;
        }

        var hitSequence = hit.collider.GetComponentInParent<Sequence>();
        if (hitSequence != sequence)
        {
            centerPool?.ClearPreview();
            return;
        }

        centerPool?.PreviewInflateDeflate(hit.point);
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

    private void SetTeleportTargetsEnabled(TeleportationArea[] targets, bool enabled)
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

    private void SetTeleportTargetsEnabled(TeleportationAnchor[] targets, bool enabled)
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

    private void HideTeleportRayReticle()
    {
        if (lineVisual != null && lineVisual.reticle != null)
        {
            lineVisual.reticle.SetActive(false);
            lineVisual.reticle = null;
        }

        if (leftTeleportation == null)
            return;

        var reticleTransform = leftTeleportation.transform.Find("Reticle");
        if (reticleTransform != null)
            reticleTransform.gameObject.SetActive(false);
    }

    private void HideTeleportAreaVisuals()
    {
        if (teleportationAreas != null)
        {
            for (int i = 0; i < teleportationAreas.Length; i++)
            {
                var area = teleportationAreas[i];
                if (area == null)
                    continue;

                var renderers = area.GetComponentsInChildren<Renderer>(true);
                for (int j = 0; j < renderers.Length; j++)
                    renderers[j].enabled = false;
            }
        }
    }

    private void ExpandTeleportAnchorTolerance()
    {
        if (teleportationAnchors == null)
            return;

        for (int i = 0; i < teleportationAnchors.Length; i++)
        {
            var anchor = teleportationAnchors[i];
            if (anchor == null)
                continue;

            var capsule = anchor.GetComponent<Collider>() as CapsuleCollider;
            if (capsule != null)
                capsule.radius = Mathf.Max(capsule.radius, AnchorSnapRadius);
        }
    }
}
