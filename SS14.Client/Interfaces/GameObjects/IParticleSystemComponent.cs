using SS14.Shared.Interfaces.GameObjects;

namespace SS14.Client.Interfaces.GameObjects
{
    public interface IParticleSystemComponent : IComponent
    {
        void AddParticleSystem(string name, bool active);
        void RemoveParticleSystem(string name);
        void SetParticleSystemActive(string name, bool active);
    }
}
