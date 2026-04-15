using System;
using System.Collections.Generic;
using System.Numerics;

namespace TvmVr2.Core.Methods.BasicTranslate
{
    public sealed class KabschSequenceDeformer
    {
        public int Neighbors { get; }
        public float InitialMaxSearchDistance { get; }

        public KabschSequenceDeformer(int neighbors = 4, float initialMaxSearchDistance = 0.1f)
        {
            Neighbors = neighbors;
            InitialMaxSearchDistance = initialMaxSearchDistance;
        }

        public Vector3[][] DeformSequence(
            int centerIndex,
            int frameIndex,
            Vector3 translation,
            Vector3[] centersBefore,
            Vector3[][] allCenters,
            GaussianCenterDeformer centerDeformer)
        {
            var frameCount = allCenters.Length;
            var deformedFrames = new Vector3[frameCount][];
            var nearestCenters = GetNearestNeighbors(allCenters[frameIndex][centerIndex], centersBefore);

            Carry(centerIndex, frameIndex, -1, frameCount, nearestCenters, translation, allCenters, deformedFrames, centerDeformer);
            Carry(centerIndex, frameIndex, +1, frameCount, nearestCenters, translation, allCenters, deformedFrames, centerDeformer);
            deformedFrames[frameIndex] = centerDeformer.DeformCenters(centerIndex, translation, allCenters[frameIndex]);

            return deformedFrames;
        }

        private void Carry(
            int centerIndex,
            int frameIndex,
            int frameDirection,
            int frameCount,
            int[] nearestCenters,
            Vector3 translation,
            Vector3[][] allCenters,
            Vector3[][] deformedFrames,
            GaussianCenterDeformer centerDeformer)
        {
            var nextFrameIndex = frameIndex + frameDirection;

            if (nextFrameIndex < 0 || nextFrameIndex >= frameCount)
                return;

            var p = Kabsch.MatrixFrom(centerIndex, nearestCenters, allCenters[frameIndex]);
            var q = Kabsch.MatrixFrom(centerIndex, nearestCenters, allCenters[nextFrameIndex]);

            var avgP = Kabsch.Avg(p);
            var avgQ = Kabsch.Avg(q);

            Kabsch.Subtract(p, avgP);
            Kabsch.Subtract(q, avgQ);

            var rotation = Kabsch.GetRotation(p, q);
            var direction = Kabsch.GetDirection(translation);
            var rotatedDirection = rotation * direction;
            var rotatedTranslation = new Vector3(rotatedDirection[0], rotatedDirection[1], rotatedDirection[2]);

            Carry(centerIndex, nextFrameIndex, frameDirection, frameCount, nearestCenters, rotatedTranslation, allCenters, deformedFrames, centerDeformer);

            deformedFrames[nextFrameIndex] = centerDeformer.DeformCenters(centerIndex, rotatedTranslation, allCenters[nextFrameIndex]);
        }

        private int[] GetNearestNeighbors(Vector3 point, Vector3[] centersBefore)
        {
            var kdTree = new KDTree(centersBefore);
            var maxSearchDistance = InitialMaxSearchDistance;
            var indices = kdTree.findAllCloserThan(point, maxSearchDistance);

            while (indices.Count < Neighbors)
            {
                maxSearchDistance *= 2;
                indices = kdTree.findAllCloserThan(point, maxSearchDistance);
            }

            var distances = new List<float>();
            for (var i = 0; i < indices.Count; i++)
            {
                distances.Add(Vector3.Distance(point, centersBefore[indices[i]]));
            }

            var distancesArr = distances.ToArray();
            var indicesArr = indices.ToArray();
            Array.Sort(distancesArr, indicesArr);
            return indicesArr;
        }
    }
}
