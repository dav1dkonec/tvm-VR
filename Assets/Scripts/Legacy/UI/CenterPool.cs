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

            DisableRuntimeMotion(centers[i]);
        }
    }

    private static void DisableRuntimeMotion(CenterUI center)
    {
        if (center.TryGetComponent<SphereWave>(out var sphereWave))
        {
            Destroy(sphereWave);
        }

        if (center.TryGetComponent<Rigidbody>(out var rigidbody))
        {
            if (!rigidbody.isKinematic)
            {
                rigidbody.linearVelocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
            }

            rigidbody.isKinematic = true;
            rigidbody.Sleep();
        }

        if (center.TryGetComponent<Collider>(out var collider))
        {
            collider.isTrigger = true;
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

            // Centers are controlled by the sequence data, not by scene physics or idle animation.
            DisableRuntimeMotion(centers[i]);
        }
    }
}
