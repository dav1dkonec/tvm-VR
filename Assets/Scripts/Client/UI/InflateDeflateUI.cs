using TvmVr2.Api.Enums;
using TvmVr2.Client.Centers;
using TvmVr2.Client.Sequence;
using UnityEngine;

public class InflateDeflateUI : MonoBehaviour
{
    public GameObject panelObject;

    private EditingMethodRuntimeSettings target;
    private Sequence sequence;
    private CenterPool centerPool;
    private CenterUI selectedReferenceCenter;

    public static bool IsAnyPickActive { get; private set; }
    public bool IsPickingReferencePoint => false;
    public bool HasSelectedReferenceCenter => selectedReferenceCenter != null;
    public CenterUI SelectedReferenceCenter => selectedReferenceCenter;

    public static bool BlockIfPickActive()
    {
        return false;
    }

    private void Awake()
    {
        target = FindFirstObjectByType<EditingMethodRuntimeSettings>();
        sequence = FindFirstObjectByType<Sequence>();
        centerPool = FindFirstObjectByType<CenterPool>();
        IsAnyPickActive = false;

        if (target != null)
        {
            target.SetInflateStrength(0.20f);
            target.SetInflateMode(0);
        }
    }

    private void Start()
    {
        SetVisible(target != null && target.CurrentMethod == MethodKind.InflateDeflate);
    }

    public void SetVisible(bool visible)
    {
        if (panelObject != null)
            panelObject.SetActive(visible);

        if (!visible)
            CancelSelection();

        if (visible)
            RefreshSelectionPreview();
    }

    public bool CanChangeParameters()
    {
        return true;
    }

    public bool CanChangeMethod()
    {
        return true;
    }

    public void SelectReferenceCenter(CenterUI center)
    {
        if (center == null || target == null || target.CurrentMethod != MethodKind.InflateDeflate)
            return;

        if (selectedReferenceCenter == center)
        {
            RefreshSelectionPreview();
            return;
        }

        ClearSelectedCenterVisual();
        selectedReferenceCenter = center;
        selectedReferenceCenter.SetPersistentSelected(true);
        RefreshSelectionPreview();
    }

    public void CancelSelection()
    {
        ClearSelectedCenterVisual();
        centerPool?.ClearPreview();
    }

    public void ApplySelectedCenter()
    {
        if (selectedReferenceCenter == null || sequence == null || target == null || target.CurrentMethod != MethodKind.InflateDeflate)
            return;

        var selectedCenterIndex = selectedReferenceCenter.centerIndex;
        CancelSelection();
        sequence.CommitInflateDeflate(selectedCenterIndex);
    }

    public void RefreshSelectionPreview()
    {
        if (target == null || target.CurrentMethod != MethodKind.InflateDeflate)
            return;

        if (centerPool == null)
            centerPool = FindFirstObjectByType<CenterPool>();

        if (selectedReferenceCenter != null)
        {
            centerPool?.PreviewInflateDeflate(selectedReferenceCenter.transform.position, selectedReferenceCenter.centerIndex);
            selectedReferenceCenter.RefreshPersistentSelectedVisual();
        }
        else
            centerPool?.ClearPreview();
    }

    private void ClearSelectedCenterVisual()
    {
        if (selectedReferenceCenter != null)
            selectedReferenceCenter.SetPersistentSelected(false);

        selectedReferenceCenter = null;
    }
}
