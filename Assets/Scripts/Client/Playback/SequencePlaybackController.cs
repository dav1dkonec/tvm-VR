namespace TvmVr2.Client.Playback
{
    /// <summary>
    /// Controls sequence playback state.
    /// </summary>
    public sealed class SequencePlaybackController
    {
        /// <summary>
        /// Starts playback.
        /// </summary>
        public void Play(ref bool isPlaying, ref float time)
        {
            isPlaying = true;
            time = 0f;
        }

        /// <summary>
        /// Pauses playback.
        /// </summary>
        public void Pause(ref bool isPlaying)
        {
            isPlaying = false;
        }

        /// <summary>
        /// Advances playback timer.
        /// </summary>
        public bool Tick(ref float time, float deltaTime, float secondsPerFrame)
        {
            if (secondsPerFrame <= 0f)
                return false;

            time += deltaTime;
            if (time <= secondsPerFrame)
                return false;

            time -= secondsPerFrame;
            return true;
        }

        /// <summary>
        /// Gets previous frame index.
        /// </summary>
        public int PreviousFrame(int currentFrame, int frameCount)
        {
            return (frameCount + currentFrame - 1) % frameCount;
        }

        /// <summary>
        /// Gets next frame index.
        /// </summary>
        public int NextFrame(int currentFrame, int frameCount)
        {
            return (currentFrame + 1) % frameCount;
        }

        /// <summary>
        /// Gets first frame index.
        /// </summary>
        public int FirstFrame()
        {
            return 0;
        }

        /// <summary>
        /// Gets last frame index.
        /// </summary>
        public int LastFrame(int frameCount)
        {
            return frameCount - 1;
        }
    }
}
