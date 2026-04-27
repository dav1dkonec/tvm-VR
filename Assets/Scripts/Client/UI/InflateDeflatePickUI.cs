using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InflateDeflatePickUI : MonoBehaviour
{
    private static readonly Color PickActiveColor = new(0.49019608f, 1f, 0.8784314f, 1f);

    public InflateDeflateUI controller;
    public Button pickPointButton;
    public Button cancelButton;

    private TMP_Text pickPointText;
    private Color pickPointDefaultTextColor;
    private bool lastPickVisualState;

    private void Awake()
    {
        if (controller == null)
            controller = FindFirstObjectByType<InflateDeflateUI>();

        CachePickPointVisuals();

        if (pickPointButton != null)
            pickPointButton.onClick.AddListener(BeginPick);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(CancelPick);
    }

    private void Start()
    {
        UpdatePickPointVisualState(force: true);
    }

    private void Update()
    {
        UpdatePickPointVisualState(force: false);
    }

    public void BeginPick()
    {
        if (controller != null)
            controller.BeginPick();
    }

    public void CancelPick()
    {
        if (controller != null)
            controller.CancelPick();
    }

    private void CachePickPointVisuals()
    {
        if (pickPointButton == null)
            return;

        pickPointText = pickPointButton.GetComponentInChildren<TMP_Text>(true);

        if (pickPointText != null)
            pickPointDefaultTextColor = pickPointText.color;
    }

    private void UpdatePickPointVisualState(bool force)
    {
        var pickActive = controller != null && controller.IsPickingReferencePoint;
        if (!force && pickActive == lastPickVisualState)
            return;

        lastPickVisualState = pickActive;

        if (pickPointButton != null)
            pickPointButton.interactable = !pickActive;

        if (pickPointText != null)
            pickPointText.color = pickActive ? PickActiveColor : pickPointDefaultTextColor;
    }
}
