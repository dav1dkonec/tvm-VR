using TMPro;
using TvmVr2.Client.Sequence;
using UnityEngine;
using UnityEngine.UI;

public class SequenceReloadButtonUI : MonoBehaviour
{
    public Sequence sequence;
    public Button button;
    public TMP_Text label;
    public string labelText = "Reset";
    public bool replaceExistingListeners = true;

    private Color labelEnabledColor;
    private Image[] iconImages;
    private Color[] iconEnabledColors;
    private bool colorsCached;

    private void Awake()
    {
        if (sequence == null)
            sequence = FindFirstObjectByType<Sequence>();

        if (button == null)
            button = GetComponent<Button>();

        if (label == null)
            label = GetComponentInChildren<TMP_Text>(true);

        CacheColors();

        if (label != null)
            label.text = labelText;

        if (button != null)
        {
            if (replaceExistingListeners)
                button.onClick = new Button.ButtonClickedEvent();

            button.onClick.AddListener(OnClicked);
        }
    }

    private void OnEnable()
    {
        if (sequence == null)
            sequence = FindFirstObjectByType<Sequence>();

        if (sequence != null)
            sequence.PendingEditsChanged += HandlePendingEditsChanged;

        RefreshState();
    }

    private void OnDisable()
    {
        if (sequence != null)
            sequence.PendingEditsChanged -= HandlePendingEditsChanged;
    }

    public void OnClicked()
    {
        if (sequence != null)
            sequence.ReloadLoadedSequence();
    }

    private void HandlePendingEditsChanged(bool hasPendingEdits)
    {
        RefreshState();
    }

    private void CacheColors()
    {
        if (colorsCached)
            return;

        colorsCached = true;
        labelEnabledColor = label != null ? label.color : Color.white;
        iconImages = GetComponentsInChildren<Image>(true);
        iconEnabledColors = new Color[iconImages.Length];
        for (int i = 0; i < iconImages.Length; i++)
            iconEnabledColors[i] = iconImages[i] != null ? iconImages[i].color : Color.white;
    }

    private void RefreshState()
    {
        var enabled = sequence != null && sequence.HasPendingEdits;

        if (button != null)
            button.interactable = enabled;

        if (label != null)
            label.color = enabled ? labelEnabledColor : new Color(labelEnabledColor.r, labelEnabledColor.g, labelEnabledColor.b, 0.45f);

        if (iconImages != null)
        {
            for (int i = 0; i < iconImages.Length; i++)
            {
                var image = iconImages[i];
                if (image == null)
                    continue;

                var baseColor = i < iconEnabledColors.Length ? iconEnabledColors[i] : image.color;
                image.color = enabled
                    ? baseColor
                    : new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * 0.45f);
            }
        }
    }
}
