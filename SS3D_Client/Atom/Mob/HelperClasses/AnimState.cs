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
        public Mob parent;
        float length;
        float timeposition;
        public bool tempdisabled;
        public bool final;

        public AnimState(AnimationState a, Mob _parent)
        {
            animationState = a;
            Disable();
            LoopOff();
            length = animationState.Length;
            timeposition = 0f;
            parent = _parent;
        }

        public void Update(float time)
        {
            if (!enabled || tempdisabled)
                return;
            if (!looping && animationState.HasEnded && !final)
            {
                animationState.TimePosition = 0;
                Disable();
                parent.AnimationComplete(); // Should really use events for this, but i'm too lazy to learn that at the moment.
                return;
            }

            animationState.AddTime(time);
        }

        public void RunOnce()
        {
            Enable();
            LoopOff();
            tempdisabled = false;
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
