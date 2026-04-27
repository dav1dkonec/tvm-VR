using System.Collections;
using TMPro;
using TvmVr2.Api.Enums;
using TvmVr2.Client.Sequence;
using UnityEngine;

public class InflateDeflateUI : MonoBehaviour
{
    private const float TransientMessageDuration = 2f;

    public GameObject panelObject;
    public TMP_Text statusText;

    private EditingMethodRuntimeSettings target;
    private GameObject transientMessageCanvas;
    private TMP_Text transientMessageText;
    private Coroutine transientMessageRoutine;
    private bool isPickingReferencePoint;

    public static bool IsAnyPickActive { get; private set; }
    public bool IsPickingReferencePoint => isPickingReferencePoint;

    public static bool BlockIfPickActive()
    {
        if (!IsAnyPickActive)
            return false;

        Object.FindFirstObjectByType<InflateDeflateUI>()?.ShowPickModeBlockedMessage();
        return true;
    }

    private void Awake()
    {
        target = FindFirstObjectByType<EditingMethodRuntimeSettings>();
        InitializeTransientMessageCanvas();

        if (target != null)
        {
            target.SetInflateRadius(0.10f);
            target.SetInflateStrength(0.20f);
            target.SetInflateMode(0);
        }
    }

    private void Start()
    {
        SetWorkflowStatus();
        SetVisible(target != null && target.CurrentMethod == MethodKind.InflateDeflate);
    }

    private void OnDisable()
    {
        if (isPickingReferencePoint)
            IsAnyPickActive = false;
    }

    public void SetVisible(bool visible)
    {
        if (panelObject != null)
            panelObject.SetActive(visible);

        if (!visible)
            CancelPickSilently();

        if (visible)
            SetWorkflowStatus();
    }

    public bool CanChangeParameters()
    {
        if (!isPickingReferencePoint)
            return true;

        ShowPickModeBlockedMessage();
        return false;
    }

    public bool CanChangeMethod()
    {
        if (!isPickingReferencePoint)
            return true;

        ShowPickModeBlockedMessage();
        return false;
    }

    public void ShowPickModeBlockedMessage()
    {
        ShowTransientMessage("Select a point or cancel first.");
    }

    public void BeginPick()
    {
        if (target == null)
            return;

        if (isPickingReferencePoint)
        {
            ShowPickModeBlockedMessage();
            return;
        }

        target.CurrentMethod = MethodKind.InflateDeflate;
        RightReferencePointRay.EnsureExists();
        isPickingReferencePoint = true;
        IsAnyPickActive = true;
    }

    public void CancelPick()
    {
        CancelPickSilently();
    }

    public void ShowPickFailed(string message)
    {
        isPickingReferencePoint = false;
        IsAnyPickActive = false;
        if (!string.IsNullOrWhiteSpace(message))
            Debug.LogWarning(message);
    }

    public void ShowPickCompleted()
    {
        isPickingReferencePoint = false;
        IsAnyPickActive = false;
    }

    private void CancelPickSilently()
    {
        if (!isPickingReferencePoint)
            return;

        isPickingReferencePoint = false;
        IsAnyPickActive = false;
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    private void SetWorkflowStatus()
    {
        SetStatus("Set parameters, press Pick Point, then use the right hand ray to aim at the mesh and press the trigger.");
    }

    private void InitializeTransientMessageCanvas()
    {
        var sequence = FindFirstObjectByType<Sequence>();
        if (sequence == null || sequence.waitCanvas == null)
            return;

        transientMessageCanvas = Instantiate(sequence.waitCanvas, sequence.waitCanvas.transform.parent);
        transientMessageCanvas.name = "Transient Interaction Message";
        transientMessageText = transientMessageCanvas.GetComponentInChildren<TMP_Text>(true);
        transientMessageCanvas.SetActive(false);
    }

    private void ShowTransientMessage(string message)
    {
        if (transientMessageCanvas == null || transientMessageText == null)
        {
            Debug.Log(message);
            return;
        }

        if (transientMessageRoutine != null)
            StopCoroutine(transientMessageRoutine);

        transientMessageRoutine = StartCoroutine(ShowTransientMessageRoutine(message));
    }

    private IEnumerator ShowTransientMessageRoutine(string message)
    {
        transientMessageText.text = message;
        transientMessageCanvas.SetActive(true);
        yield return new WaitForSecondsRealtime(TransientMessageDuration);
        transientMessageCanvas.SetActive(false);
        transientMessageRoutine = null;
    }
}
