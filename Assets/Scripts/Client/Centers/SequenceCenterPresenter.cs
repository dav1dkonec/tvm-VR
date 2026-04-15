namespace TvmVr2.Client.Centers
{
    public sealed class SequenceCenterPresenter
    {
        public void EnsureInitialized(CenterPool centerPool, int centerCount)
        {
            if (centerPool == null || centerCount < 0)
                return;

            if (centerPool.centers == null || centerPool.centers.Length != centerCount)
                centerPool.Initialize(centerCount);
        }

        public void SyncPositions(CenterPool centerPool, System.Numerics.Vector3[] positions)
        {
            if (centerPool == null || positions == null)
                return;

            EnsureInitialized(centerPool, positions.Length);
            centerPool.SetPositions(positions);
        }
    }
}
