using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mogre;

namespace SS3D.Atom.Mob.HelperClasses
{
    public class AnimState
    {
        private AnimationState animationState;
        public bool looping;
        public bool enabled;
        float length;
        float timeposition;

        public AnimState(AnimationState a)
        {
            animationState = a;
            Disable();
            LoopOff();
            length = animationState.Length;
            timeposition = 0f;
        }

        public void Update(float time)
        {
            if (!enabled)
                return;
            if (!looping && animationState.HasEnded)
            {
                animationState.TimePosition = 0;
                Disable();
                return;
            }

            animationState.AddTime(time);
        }

        public void RunOnce()
        {
            Enable();
            LoopOff();
        }

        public void Enable()
        {
            enabled = true;
            animationState.Enabled = enabled;
        }

        public void Disable()
        {
            enabled = false;
            animationState.Enabled = enabled;
        }

        public void LoopOn()
        {
            looping = true;
            animationState.Loop = looping;
        }

        public void LoopOff()
        {
            looping = false;
            animationState.Loop = looping;
        }
    }
}
