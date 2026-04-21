using TMPro;
using TvmVr2.Api.Enums;
using TvmVr2.Client.Sequence;
using UnityEngine;

public class InflateDeflateUI : MonoBehaviour
{
    public GameObject panelObject;
    public TMP_Text statusText;

    private EditingMethodRuntimeSettings target;
    private ActivateTeleportationRay teleportRay;
    private bool isPickingReferencePoint;

    public bool IsPickingReferencePoint => isPickingReferencePoint;

    private void Awake()
    {
        target = FindFirstObjectByType<EditingMethodRuntimeSettings>();
        teleportRay = FindFirstObjectByType<ActivateTeleportationRay>();

        if (target != null)
        {
            target.SetInflateRadius(0.10f);
            target.SetInflateStrength(0.20f);
            target.SetInflateMode(0);
        }
    }

    private void Start()
    {
        SetStatus("Set parameters, press Pick Point, then use the left hand ray to aim at the mesh and release the teleport trigger.");
        SetVisible(target != null && target.CurrentMethod == MethodKind.InflateDeflate);
    }

    public void SetVisible(bool visible)
    {
        if (panelObject != null)
            panelObject.SetActive(visible);

        if (!visible)
            CancelPickSilently();

        if (visible)
            SetStatus("Set parameters, press Pick Point, then use the left hand ray to aim at the mesh and release the teleport trigger.");
    }

    public bool CanChangeParameters()
    {
        if (!isPickingReferencePoint)
            return true;

        SetStatus("Cannot change parameters while picking a reference point. Cancel point selection first.");
        return false;
    }

    public void BeginPick()
    {
        if (target == null || teleportRay == null)
            return;

        target.CurrentMethod = MethodKind.InflateDeflate;
        isPickingReferencePoint = true;
        teleportRay.BeginInflateDeflatePick();
        SetStatus("Pick mode is active. Use the left hand ray, hold the teleport trigger, aim at the mesh and release.");
    }

    public void CancelPick()
    {
        CancelPickSilently();
        SetStatus("Selection cancelled. Teleport works normally again.");
    }

    public void ShowPickFailed(string message)
    {
        isPickingReferencePoint = false;
        SetStatus(message);
    }

    public void ShowPickCompleted()
    {
        isPickingReferencePoint = false;
        SetStatus("Reference point selected. The edit was sent to the method pipeline.");
    }

    private void CancelPickSilently()
    {
        if (!isPickingReferencePoint && teleportRay == null)
            return;

        if (teleportRay != null)
            teleportRay.CancelInflateDeflatePick();

        isPickingReferencePoint = false;
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
}
