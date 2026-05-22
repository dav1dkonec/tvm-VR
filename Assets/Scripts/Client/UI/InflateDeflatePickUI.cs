using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Inflate/deflate selection buttons.
/// </summary>
public class InflateDeflatePickUI : MonoBehaviour
{
    /// <summary>
    /// Inflate/deflate panel controller.
    /// </summary>
    public InflateDeflateUI controller;

    /// <summary>
    /// Apply selected center button.
    /// </summary>
    public Button pickPointButton;

    /// <summary>
    /// Cancel selected center button.
    /// </summary>
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

    /// <summary>
    /// Applies current selection.
    /// </summary>
    public void ApplySelection()
    {
        controller?.ApplySelectedCenter();
        UpdateButtonState();
    }

    /// <summary>
    /// Cancels current selection.
    /// </summary>
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
