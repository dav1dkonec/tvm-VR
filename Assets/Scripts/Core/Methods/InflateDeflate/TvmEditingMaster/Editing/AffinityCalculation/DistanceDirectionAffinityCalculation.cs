using MathNet.Numerics.LinearAlgebra.Single;
using System;
using System.Numerics;
using System.Threading.Tasks;

namespace TVMEditor.Editing.AffinityCalculation
{
    public class DistanceDirectionAffinityCalculation : IAffinityCalculation
    {
        public float ShapeDistance { get; set; } = 6f;
        public float ShapeDirection { get; set; } = 0f;
        public int Power { get; set; } = 1;

        private float[,] Affinity { get; set; }

        public float[,] CalculateCentersAffinity(Vector3[][] volumeCenters)
        {
            float[,] centersAffinity = null;

            if (volumeCenters == null)
                return centersAffinity;

            var distances = new float[volumeCenters[0].Length, volumeCenters[0].Length];
            var directionDifferences = new float[volumeCenters[0].Length, volumeCenters[0].Length];
            centersAffinity = new float[volumeCenters[0].Length, volumeCenters[0].Length];

            Parallel.For(0, volumeCenters.Length, f =>
            {
                for (var i = 0; i < volumeCenters[f].Length; i++)
                {
                    for (var j = i + 1; j < volumeCenters[f].Length; j++)
                    {
                        var center1 = volumeCenters[f][i];
                        var center2 = volumeCenters[f][j];
                        var distance = (center2 - center1).Length();

                        if (distance > distances[i, j])
                        {
                            distances[i, j] = distance;
                            distances[j, i] = distance;
                        }
                    }
                }
            });

            Parallel.For(0, volumeCenters.Length - 1, f =>
            {
                for (var i = 0; i < volumeCenters[f].Length; i++)
                {
                    for (var j = i + 1; j < volumeCenters[f].Length; j++)
                    {
                        var centerSrc1 = volumeCenters[f][i];
                        var centerSrc2 = volumeCenters[f][j];
                        var centerDest1 = volumeCenters[f + 1][i];
                        var centerDest2 = volumeCenters[f + 1][j];
                        var u1 = centerDest1 - centerSrc1;
                        var u2 = centerDest2 - centerSrc2;
                        var directionDifference = System.Math.Max(-Vector3.Dot(u1, u2), 0f);

                        if (directionDifference > directionDifferences[i, j])
                        {
                            directionDifferences[i, j] = directionDifference;
                            directionDifferences[j, i] = directionDifference;
                        }
                    }
                }
            });

            Parallel.For(0, volumeCenters[0].Length, i =>
            {
                centersAffinity[i, i] = 1;
                for (var j = i + 1; j < volumeCenters[0].Length; j++)
                {
                    centersAffinity[i, j] = centersAffinity[j, i] =
                        Rbf(ShapeDistance, distances[i, j]) * Rbf(ShapeDirection, directionDifferences[i, j]);
                }
            });

            PowerAffinity(centersAffinity, volumeCenters, Power);
            Affinity = centersAffinity;
            return centersAffinity;
        }

        public float[,] GetCentersAffinity()
        {
            return Affinity;
        }

        public void SetCentersAffinity(float[,] affinity)
        {
            Affinity = affinity;
        }

        private float Rbf(float shape, float dist)
        {
            return (float)System.Math.Exp(-shape * shape * dist * dist);
        }

        private void PowerAffinity(float[,] affinity, Vector3[][] volumeCenters, int power)
        {
            var matrix = new DenseMatrix(affinity.GetLength(0), affinity.GetLength(1));
            for (var i = 0; i < volumeCenters[0].Length; i++)
            {
                matrix[i, i] = affinity[i, i];
                for (var j = i + 1; j < volumeCenters[0].Length; j++)
                {
                    matrix[i, j] = affinity[i, j];
                    matrix[j, i] = affinity[j, i];
                }
            }

            var result = matrix.Clone();
            for (var i = 1; i < power; i++)
            {
                result *= matrix;
            }

            for (var i = 0; i < volumeCenters[0].Length; i++)
            {
                affinity[i, i] = matrix[i, i];
                for (var j = i + 1; j < volumeCenters[0].Length; j++)
                {
                    affinity[i, j] = matrix[i, j];
                    affinity[j, i] = matrix[j, i];
                }
            }
        }
    }
}
