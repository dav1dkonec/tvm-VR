using UnityEngine;
using UnityEngine.UI;

public class InflateDeflatePickUI : MonoBehaviour
{
    public InflateDeflateUI controller;
    public Button pickPointButton;
    public Button cancelButton;

    private void Awake()
    {
        if (controller == null)
            controller = FindFirstObjectByType<InflateDeflateUI>();

        if (pickPointButton != null)
            pickPointButton.onClick.AddListener(ApplySelection);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(CancelSelection);
    }

    private void Start()
    {
        UpdateButtonState();
    }

    private void Update()
    {
        UpdateButtonState();
    }

    public void ApplySelection()
    {
        controller?.ApplySelectedCenter();
        UpdateButtonState();
    }

    public void CancelSelection()
    {
        controller?.CancelSelection();
        UpdateButtonState();
    }

    private void UpdateButtonState()
    {
        var hasSelection = controller != null && controller.HasSelectedReferenceCenter;

        if (pickPointButton != null)
            pickPointButton.interactable = hasSelection;

        if (cancelButton != null)
            cancelButton.interactable = hasSelection;
    }
}
