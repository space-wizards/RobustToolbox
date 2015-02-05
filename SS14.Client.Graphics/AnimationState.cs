using System;

namespace SS14.Client.Graphics
{
    public class AnimationState
    {
        private AnimationInfo _info;
        public int CurrentFrame { get; private set; }
        public float CurrentTime { get; private set; }
        public float MaxTime { get { return ((float)1/_info.FPS)*(_info.Frames-1); } }
        public bool Loop { get; set; }
        public bool Enabled { get; set; }
        public string Name { get { return _info.Name; } }

        public AnimationState(AnimationInfo info)
        {
            _info = info;
        }

        public void Reset()
        {
            CurrentTime = 0;
            Enabled = false;
            Loop = false;
            CurrentFrame = 0;
        }

        public void AddTime(float time)
        {
            CurrentTime += time;
            if (CurrentTime >= MaxTime)
            {
                if (Loop)
                    CurrentTime -= MaxTime;
                else
                    CurrentTime = MaxTime;
            }
            CurrentFrame = (int)Math.Floor(CurrentTime*_info.FPS);
        }

        public void SetTime(float time)
        {
            CurrentTime = time;
            CurrentFrame = (int) Math.Floor(CurrentTime*_info.FPS);
        }
    }
}
