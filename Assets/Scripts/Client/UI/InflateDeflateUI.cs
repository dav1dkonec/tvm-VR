using TvmVr2.Api.Enums;
using TvmVr2.Client.Centers;
using TvmVr2.Client.Sequence;
using UnityEngine;

/// <summary>
/// Inflate/deflate panel controller.
/// </summary>
public class InflateDeflateUI : MonoBehaviour
{
    /// <summary>
    /// Inflate/deflate panel object.
    /// </summary>
    public GameObject panelObject;

    private EditingMethodRuntimeSettings target;
    private Sequence sequence;
    private CenterPool centerPool;
    private CenterUI selectedReferenceCenter;

    /// <summary>
    /// Whether any inflate/deflate pick action is active.
    /// </summary>
    public static bool IsAnyPickActive { get; private set; }

    /// <summary>
    /// Whether reference point picking is active.
    /// </summary>
    public bool IsPickingReferencePoint => false;

    /// <summary>
    /// Whether a reference center is selected.
    /// </summary>
    public bool HasSelectedReferenceCenter => selectedReferenceCenter != null;

    /// <summary>
    /// Selected reference center.
    /// </summary>
    public CenterUI SelectedReferenceCenter => selectedReferenceCenter;

    /// <summary>
    /// Blocks an action when picking is active.
    /// </summary>
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

    /// <summary>
    /// Sets panel visibility.
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (panelObject != null)
            panelObject.SetActive(visible);

        if (!visible)
            CancelSelection();

        if (visible)
            RefreshSelectionPreview();
    }

    /// <summary>
    /// Checks whether parameters can change.
    /// </summary>
    public bool CanChangeParameters()
    {
        return true;
    }

    /// <summary>
    /// Checks whether method can change.
    /// </summary>
    public bool CanChangeMethod()
    {
        return true;
    }

    /// <summary>
    /// Selects reference center.
    /// </summary>
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

    /// <summary>
    /// Cancels selected reference center.
    /// </summary>
    public void CancelSelection()
    {
        ClearSelectedCenterVisual();
        centerPool?.ClearPreview();
    }

    /// <summary>
    /// Applies edit to selected reference center.
    /// </summary>
    public void ApplySelectedCenter()
    {
        if (selectedReferenceCenter == null || sequence == null || target == null || target.CurrentMethod != MethodKind.InflateDeflate)
            return;

        var selectedCenterIndex = selectedReferenceCenter.centerIndex;
        CancelSelection();
        sequence.CommitInflateDeflate(selectedCenterIndex);
    }

    /// <summary>
    /// Refreshes selected center preview.
    /// </summary>
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
