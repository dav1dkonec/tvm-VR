namespace TvmVr2.Client.Centers
{
    /// <summary>
    /// Presents sequence centers in the scene.
    /// </summary>
    public sealed class SequenceCenterPresenter
    {
        /// <summary>
        /// Ensures center pool size.
        /// </summary>
        public void EnsureInitialized(CenterPool centerPool, int centerCount)
        {
            if (centerPool == null || centerCount < 0)
                return;

            centerPool.PrepareForSequence(centerCount);
        }

        /// <summary>
        /// Synchronizes center positions.
        /// </summary>
        public void SyncPositions(CenterPool centerPool, System.Numerics.Vector3[] positions)
        {
            if (centerPool == null || positions == null)
                return;

            EnsureInitialized(centerPool, positions.Length);
            centerPool.SetPositions(positions);
        }
    }
}
