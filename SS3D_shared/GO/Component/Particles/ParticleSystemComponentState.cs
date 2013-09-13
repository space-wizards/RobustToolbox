using System;

namespace SS13_Shared.GO.Component.Particles
{
    [Serializable]
    public class ParticleSystemComponentState : ComponentState
    {
        public bool Active;
        public Vector4 StartColor;
        public Vector4 EndColor;

        public ParticleSystemComponentState(bool active, Vector4 startColor, Vector4 endColor)
            : base(ComponentFamily.Particles)
        {
            Active = active;
            StartColor = startColor;
            EndColor = endColor;
        }
    }
}