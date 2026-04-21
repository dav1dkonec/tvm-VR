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
            pickPointButton.onClick.AddListener(BeginPick);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(CancelPick);
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
}
