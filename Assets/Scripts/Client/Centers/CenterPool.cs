using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A pool of center objects to be used to display sequence frames
/// </summary>
public class CenterPool : MonoBehaviour
{
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

    /// <summary>
    /// Initializes the centers and animates them
    /// </summary>
    void Start()
    {
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
}
