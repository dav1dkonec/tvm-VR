using System.Collections;
using System.Collections.Generic;
using TvmVr2.Api.Enums;
using TvmVr2.Client.Sequence;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// A pool of center objects to be used to display sequence frames
/// </summary>
public class CenterPool : MonoBehaviour, ICenterHoverListener
{
    private const float InflateStrengthPreviewReference = 0.5f;
    private const float InflateStrengthPreviewMinimum = 0.15f;
    private const float InflatePreviewMinimumVisibleIntensity = 0.35f;

    /// <summary>
    /// Center game object prefab
    /// </summary>
    public GameObject prefab;

    /// <summary>
    /// Initial sphere wave animation settings
    /// </summary>
    public SphereWaveSettings settings;

    /// <summary>
    /// The center pool
    /// </summary>
    public CenterUI[] centers;
    private EditingMethodRuntimeSettings methodSettings;

    /// <summary>
    /// Initializes the centers and animates them
    /// </summary>
    void Start()
    {
        methodSettings = FindFirstObjectByType<EditingMethodRuntimeSettings>();
        CenterUI.RegisterListener(this);
        Idle();
    }

    /// <summary>
    /// Initializes animated centers that aren't displaying any sequence yet
    /// </summary>
    public void Idle()
    {
        Initialize(1000);
        foreach (var c in centers)
        {
            var sphereWave = c.gameObject.AddComponent<SphereWave>();
            sphereWave.settings = settings;
        }
    }

    /// <summary>
    /// Initializes a pool of centers of the given size
    /// </summary>
    /// <param name="count"></param>
    public void Initialize(int count)
    {
        CenterUI.ClearActiveSelections();
        ClearPreview();

        // Destroy the old pool
        if (centers != null)
        {
            for (int i = 0; i < centers.Length; i++)
            {
                Destroy(centers[i].gameObject);
            }
        }

        // Spawn a new pool
        centers = new CenterUI[count];

        for (int i = 0; i < count; i++)
        {
            var c = Instantiate(prefab, transform);
            centers[i] = c.GetComponent<CenterUI>();
            centers[i].transform.localPosition = centers[i].transform.localPosition + MathUtils.RandomUnitVector3() * 0.5f;
            centers[i].centerIndex = i;
        }
    }

    public void PrepareForSequence(int count)
    {
        ClearPreview();

        if (count < 0)
            return;

        if (centers == null || centers.Length != count)
        {
            Initialize(count);
            return;
        }

        for (int i = 0; i < centers.Length; i++)
        {
            if (centers[i] == null)
            {
                Initialize(count);
                return;
            }

            var sphereWave = centers[i].GetComponent<SphereWave>();
            if (sphereWave != null)
                Destroy(sphereWave);
        }
    }

    /// <summary>
    /// Sets positions of the centers
    /// </summary>
    /// <param name="positions">Array of new center positions</param>
    public void SetPositions(System.Numerics.Vector3[] positions)
    {
        if (centers == null || positions.Length != centers.Length)
        {
            Debug.LogError("Center count doesn't equal position count. (Likely waiting for the sequence to load.)");
            return;
        }

        for (int i = 0; i < centers.Length; i++)
        {
            centers[i].transform.localPosition = new Vector3(
                positions[i].X,
                positions[i].Y,
                positions[i].Z);
        }
    }

    public void Notify(CenterUI center, bool hovering)
    {
        if (!hovering)
        {
            ClearPreview();
            return;
        }

        if (center == null)
            return;

        if (methodSettings == null)
            methodSettings = FindFirstObjectByType<EditingMethodRuntimeSettings>();

        var sigma = methodSettings != null ? methodSettings.CenterSigma : 1f;
        var from = center.transform.position;

        for (int i = 0; i < centers.Length; i++)
        {
            var targetCenter = centers[i];
            if (targetCenter == null)
                continue;

            var distance = Vector3.Distance(from, targetCenter.transform.position);
            var intensity = Mathf.Exp(-sigma * distance);
            ApplyPreviewIntensity(targetCenter, intensity);
        }
    }

    public void PreviewInflateDeflate(Vector3 referencePoint)
    {
        if (centers == null || centers.Length == 0)
            return;

        if (methodSettings == null)
            methodSettings = FindFirstObjectByType<EditingMethodRuntimeSettings>();

        var radius = methodSettings != null ? methodSettings.InflateRadius : 0.08f;
        var strength = methodSettings != null ? methodSettings.InflateStrength : 0.02f;
        if (radius <= 0f || strength <= 0f)
        {
            ClearPreview();
            return;
        }

        var strengthFactor = Mathf.Lerp(
            InflateStrengthPreviewMinimum,
            1f,
            Mathf.Clamp01(strength / InflateStrengthPreviewReference));

        for (int i = 0; i < centers.Length; i++)
        {
            var targetCenter = centers[i];
            if (targetCenter == null)
                continue;

            var distance = Vector3.Distance(referencePoint, targetCenter.transform.position);
            if (distance > radius)
            {
                ApplyPreviewIntensity(targetCenter, 0f);
                continue;
            }

            var falloff = Mathf.Pow(1f - (distance / radius), 0.65f);
            var visibleIntensity = Mathf.Lerp(
                InflatePreviewMinimumVisibleIntensity,
                1f,
                falloff * strengthFactor);
            ApplyPreviewIntensity(targetCenter, visibleIntensity);
        }
    }

    public void ClearPreview()
    {
        if (centers == null)
            return;

        for (int i = 0; i < centers.Length; i++)
        {
            var center = centers[i];
            if (center == null || center.normalMaterial == null)
                continue;

            ApplyColor(center, center.normalColor);
        }
    }

    public void SetInteractionEnabled(bool enabled)
    {
        if (centers == null)
            return;

        if (!enabled)
            CenterUI.ClearActiveSelections();

        for (int i = 0; i < centers.Length; i++)
        {
            if (centers[i] == null)
                continue;

            var collider = centers[i].GetComponent<Collider>();
            if (collider != null)
                collider.enabled = enabled;

            var grabInteractable = centers[i].GetComponent<XRGrabInteractable>();
            if (grabInteractable != null)
                grabInteractable.enabled = enabled;
        }
    }

    private static void ApplyPreviewIntensity(CenterUI center, float intensity)
    {
        if (center == null || center.normalMaterial == null)
            return;

        var previewColor = Color.Lerp(center.normalColor, center.highlightedColor, Mathf.Clamp01(intensity));
        ApplyColor(center, previewColor);
    }

    private static void ApplyColor(CenterUI center, Color color)
    {
        if (center.normalMaterial.HasProperty("_EmissionColor"))
            center.normalMaterial.SetColor("_EmissionColor", color);

        if (center.normalMaterial.HasProperty("_BaseColor"))
            center.normalMaterial.SetColor("_BaseColor", color);
        else if (center.normalMaterial.HasProperty("_Color"))
            center.normalMaterial.SetColor("_Color", color);
    }
}
