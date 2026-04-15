using System;
using System.Diagnostics;
using System.Numerics;
using UnityEngine.UI;

/// <summary>
/// Implementation of center deformation by using a Gaussian falloff function
/// </summary>
public class GaussianCenterDeformation : CenterDeformation, ICenterHoverListener
{
    /// <summary>
    /// Gaussian shape parameter
    /// </summary>
    public float Sigma = 1f;

    /// <summary>
    /// Center pool for the preview effect
    /// </summary>
    public CenterPool centerPool;

    /// <summary>
    ///  UI Sigma control
    /// </summary>
    public Slider sigmaSlider;

    /// <summary>
    /// The timer keeps track of time spent in the class
    /// </summary>
    public long timer;

    /// <summary>
    /// Initialization
    /// </summary>
    void Start()
    {
        CenterUI.RegisterListener(this);
        sigmaSlider.value = SigmaToSlider(Sigma);
    }

    /// <summary>
    /// Deforms the centers within one frame by using a Gaussian falloff function
    /// </summary>
    /// <param name="centerIndex">Translated center index</param>
    /// <param name="translation">Translation vector</param>
    /// <param name="centers">Center set</param>
    /// <returns>Deformed center positions</returns>
    public override Vector3[] DeformCenters(int centerIndex, Vector3 translation, Vector3[] centers)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        Vector3[] deformedCenters = new Vector3[centers.Length];

        Vector3 translatedCenter = centers[centerIndex];
        Vector3 targetCenter;

        for (int i = 0; i < centers.Length; i++)
        {
            targetCenter = centers[i];
            float strength = GetGaussianFalloff(translatedCenter, targetCenter);
            deformedCenters[i] = targetCenter + translation * strength;
        }

        stopwatch.Stop();
        timer += stopwatch.ElapsedMilliseconds;
        
        return deformedCenters;
    }

    /// <summary>
    /// Calculates the Gaussian function value for two points
    /// </summary>
    /// <param name="from">Source vertex</param>
    /// <param name="to">Target vertex</param>
    /// <returns>Gaussian value</returns>
    private float GetGaussianFalloff(Vector3 from, Vector3 to)
    {
        return UnityEngine.Mathf.Exp(-Sigma * Vector3.Distance(from, to));
    }

    /// <summary>
    /// Receive hover notification
    /// </summary>
    /// <param name="center">Center</param>
    /// <param name="hovering">True on enter, false on exit</param>
    public void Notify(CenterUI center, bool hovering)
    {
        if (hovering)
        {
            var up = center.transform.position;
            var from = new Vector3(up.x, up.y, up.z);

            foreach (var c in centerPool.centers)
            {
                up = c.transform.position;
                var to = new Vector3(up.x, up.y, up.z);
                float strength = GetGaussianFalloff(from, to);
                c.normalMaterial.SetColor("_EmissionColor", UnityEngine.Color.Lerp(c.normalColor, c.highlightedColor, strength));
            }
        }
        else
        {
            foreach(var c in centerPool.centers)
            {
                c.normalMaterial.SetColor("_EmissionColor", c.normalColor);
            }
        }

        UnityEngine.DynamicGI.UpdateEnvironment();
    }

    /// <summary>
    /// Sets the Sigma value
    /// </summary>
    public void SetSigma()
    {
        Sigma = SigmaFromSlider(sigmaSlider.value);
    }

    /// <summary>
    /// Reset the timer which calculates how much time had been spent in the class
    /// </summary>
    public override void ResetTimer()
    {
        timer = 0;
    }


    private float SigmaFromSlider(float value)
    {
        var min = sigmaSlider.minValue;
        var max = sigmaSlider.maxValue;
        return max - value + min;
    }

    private float SigmaToSlider(float sigma)
    {
        return SigmaFromSlider(sigma);
    }
}
