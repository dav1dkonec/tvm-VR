using System;
using System.Collections.Generic;
using System.Linq;
using KdTree.Math;

namespace KdTree
{
    public sealed class KdTree<TCoord, TValue>
    {
        private readonly List<KdTreeNode<TCoord, TValue>> _nodes = new List<KdTreeNode<TCoord, TValue>>();

        public KdTree(int dimensions, ITypeMath<TCoord> typeMath)
        {
        }

        public void Add(TCoord[] point, TValue value)
        {
            _nodes.Add(new KdTreeNode<TCoord, TValue>(point, value));
        }

        public KdTreeNode<TCoord, TValue>[] GetNearestNeighbours(TCoord[] point, int count)
        {
            if (count <= 0)
                return Array.Empty<KdTreeNode<TCoord, TValue>>();

            var ordered = _nodes
                .OrderBy(node => SquaredDistance(point, node.Point))
                .Take(count)
                .ToArray();

            if (ordered.Length == count)
                return ordered;

            var padded = new KdTreeNode<TCoord, TValue>[count];
            Array.Copy(ordered, padded, ordered.Length);
            return padded;
        }

        private static double SquaredDistance(TCoord[] a, TCoord[] b)
        {
            var length = System.Math.Min(a.Length, b.Length);
            var sum = 0d;
            for (var i = 0; i < length; i++)
            {
                var da = Convert.ToDouble(a[i]);
                var db = Convert.ToDouble(b[i]);
                var diff = da - db;
                sum += diff * diff;
            }

            return sum;
        }
    }
}
