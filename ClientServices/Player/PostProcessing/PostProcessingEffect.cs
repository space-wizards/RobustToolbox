using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GorgonLibrary.Graphics;

namespace ClientServices.Player.PostProcessing
{
    public delegate void PostProcessingEffectExpired(PostProcessingEffect p);

    public class PostProcessingEffect
    {
        protected float _duration;

        public PostProcessingEffect(float duration)
        {
            _duration = duration;
        }

        public event PostProcessingEffectExpired OnExpired;

        public virtual void ProcessImage(RenderImage image)
        {
        }

        public virtual void Update(float frameTime)
        {
            _duration -= frameTime;
            if(_duration <= 0)
                Expired();
        }

        protected void Expired()
        {
            OnExpired(this);
        }
    }
}
