namespace TvmVr2.Client.Playback
{
    public sealed class SequencePlaybackController
    {
        public void Play(ref bool isPlaying, ref float time)
        {
            isPlaying = true;
            time = 0f;
        }

        public void Pause(ref bool isPlaying)
        {
            isPlaying = false;
        }

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

        public int PreviousFrame(int currentFrame, int frameCount)
        {
            return (frameCount + currentFrame - 1) % frameCount;
        }

        public int NextFrame(int currentFrame, int frameCount)
        {
            return (currentFrame + 1) % frameCount;
        }

        public int FirstFrame()
        {
            return 0;
        }

        public int LastFrame(int frameCount)
        {
            return frameCount - 1;
        }
    }
}
