namespace KdTree
{
    public sealed class KdTreeNode<TCoord, TValue>
    {
        public KdTreeNode(TCoord[] point, TValue value)
        {
            Point = point;
            Value = value;
        }

        public TCoord[] Point { get; }
        public TValue Value { get; }
    }
}
