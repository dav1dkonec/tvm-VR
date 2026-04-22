using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the sequence mesh material
/// </summary>
public class TransparencyUI : MonoBehaviour
{
    /// <summary>
    /// Transparency ON icon
    /// </summary>
    public Sprite iconAlphaOn;
    /// <summary>
    /// Transparency OFF icon
    /// </summary>
    public Sprite iconAlphaOff;

    /// <summary>
    /// Transparent material
    /// </summary>
    public Material alphaMaterial;

    /// <summary>
    /// Opaque material
    /// </summary>
    public Material normalMaterial;

    /// <summary>
    /// Image on the toggle button
    /// </summary>
    public Image alphaToggle;

    /// <summary>
    /// Slider control
    /// </summary>
    public Slider alphaSlider;

    /// <summary>
    /// Sequence mesh renderer
    /// </summary>
    public MeshRenderer meshRenderer;

    /// <summary>
    /// True if transparency is enabled
    /// </summary>
    bool alphaOn;

    /// <summary>
    /// Initialization
    /// </summary>
    void Start()
    {
        alphaOn = true;
        alphaToggle.sprite = iconAlphaOn;
        alphaSlider.interactable = true;
        meshRenderer.sharedMaterial = alphaMaterial;
        alphaSlider.value = 0.5f;
        var color = alphaMaterial.color;
        alphaMaterial.color = new Color(color.r, color.g, color.b, alphaSlider.value);
    }

    /// <summary>
    /// Changes alpha value based on slider movement
    /// </summary>
    public void ValueChanged()
    {
        if (InflateDeflateUI.BlockIfPickActive())
            return;

        var color = alphaMaterial.color;
        alphaMaterial.color = new Color(color.r, color.g, color.b, alphaSlider.value);
    }

    /// <summary>
    /// Toggles transparency on or off
    /// </summary>
    public void Toggle()
    {
        if (InflateDeflateUI.BlockIfPickActive())
            return;

        if (alphaOn)
        {
            alphaOn = false;
            alphaToggle.sprite = iconAlphaOff;
            alphaSlider.interactable = false;
            meshRenderer.sharedMaterial = normalMaterial;
        }
        else
        {
            alphaOn = true;
            alphaToggle.sprite = iconAlphaOn;
            alphaSlider.interactable = true;
            meshRenderer.sharedMaterial = alphaMaterial;
        }
    }
}
