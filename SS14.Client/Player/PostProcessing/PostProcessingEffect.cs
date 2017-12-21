/*
using SS14.Client.Graphics.Render;

namespace SS14.Client.Player.PostProcessing
{
    public delegate void PostProcessingEffectExpired(PostProcessingEffect p);

    public class PostProcessingEffect
    {
        protected bool CanExpire = false;
        protected float _duration;
        protected float _frameTime;

        public PostProcessingEffect(float duration)
        {
            if (duration <= 0)
                CanExpire = false;
            else
                CanExpire = true;
            _duration = duration;
        }

        public event PostProcessingEffectExpired OnExpired;

        public virtual void ProcessImage(RenderImage image)
        {
        }

        public virtual void Update(float frameTime)
        {
            _duration -= frameTime;
            _frameTime = frameTime;
            if (!CanExpire)
                return;
            if (_duration <= 0)
                Expired();
        }

        protected void Expired()
        {
            OnExpired(this);
        }
    }
}
*/
