using System.Collections.Generic;
using UnityEngine;

public readonly struct InflateDeflateRayCandidate
{
    public InflateDeflateRayCandidate(int centerIndex, Vector3 worldPosition, float distanceAlongRay, float distanceToRay)
    {
        CenterIndex = centerIndex;
        WorldPosition = worldPosition;
        DistanceAlongRay = distanceAlongRay;
        DistanceToRay = distanceToRay;
    }

    public int CenterIndex { get; }
    public Vector3 WorldPosition { get; }
    public float DistanceAlongRay { get; }
    public float DistanceToRay { get; }
}

public static class InflateDeflateRayCandidateSelector
{
    public static List<InflateDeflateRayCandidate> SelectCandidates(
        Ray ray,
        CenterUI[] centers,
        float minDistanceAlongRay,
        float maxDistanceAlongRay,
        float tubeRadius)
    {
        var candidates = new List<InflateDeflateRayCandidate>();
        if (centers == null || centers.Length == 0 || tubeRadius <= 0f)
            return candidates;

        Vector3 direction = ray.direction.normalized;
        float tubeRadiusSquared = tubeRadius * tubeRadius;

        for (int i = 0; i < centers.Length; i++)
        {
            var center = centers[i];
            if (center == null)
                continue;

            Vector3 toCenter = center.transform.position - ray.origin;
            float distanceAlongRay = Vector3.Dot(toCenter, direction);
            if (distanceAlongRay < minDistanceAlongRay || distanceAlongRay > maxDistanceAlongRay)
                continue;

            Vector3 closestPoint = ray.origin + direction * distanceAlongRay;
            float distanceToRaySquared = (center.transform.position - closestPoint).sqrMagnitude;
            if (distanceToRaySquared > tubeRadiusSquared)
                continue;

            candidates.Add(new InflateDeflateRayCandidate(
                center.centerIndex,
                center.transform.position,
                distanceAlongRay,
                Mathf.Sqrt(distanceToRaySquared)));
        }

        candidates.Sort(static (left, right) =>
        {
            int depthComparison = left.DistanceAlongRay.CompareTo(right.DistanceAlongRay);
            return depthComparison != 0 ? depthComparison : left.DistanceToRay.CompareTo(right.DistanceToRay);
        });

        return candidates;
    }
}
