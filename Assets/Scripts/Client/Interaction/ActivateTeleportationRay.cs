using UnityEngine;
using UnityEngine.InputSystem;
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
    private const float FullPressThreshold = 0.1f;

    /// <summary>
    /// Teleportation ray object
    /// </summary>
    public GameObject leftTeleportation;

    /// <summary>
    /// Activation input action property
    /// </summary>
    public InputActionProperty leftActivate;

    private TeleportationArea[] teleportationAreas;
    private TeleportationAnchor[] teleportationAnchors;
    private XRInteractorLineVisual lineVisual;

    void Awake()
    {
        if (leftTeleportation != null)
            lineVisual = leftTeleportation.GetComponent<XRInteractorLineVisual>();
        teleportationAreas = FindObjectsByType<TeleportationArea>(FindObjectsSortMode.None);
        teleportationAnchors = FindObjectsByType<TeleportationAnchor>(FindObjectsSortMode.None);

        ConfigureTeleportRayReticle();
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

        leftTeleportation.SetActive(leftActivate.action.ReadValue<float>() >= FullPressThreshold);
    }

    private void ConfigureTeleportRayReticle()
    {
        if (leftTeleportation == null)
            return;

        var reticleTransform = leftTeleportation.transform.Find("Reticle");
        if (reticleTransform == null)
            return;

        reticleTransform.gameObject.SetActive(true);

        if (lineVisual != null)
            lineVisual.reticle = reticleTransform.gameObject;

        var renderer = reticleTransform.GetComponent<Renderer>();
        if (renderer == null)
            return;

        var material = Resources.Load<Material>("Materials/Teleporter/Teleport Anchor");
        if (material != null)
        {
            renderer.sharedMaterial = material;
        }
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
