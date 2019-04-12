using Robust.Shared.Interfaces.GameObjects;

namespace Robust.Server.Interfaces.GameObjects
{
    public interface IParticleSystemComponent : IComponent
    {
        void AddParticleSystem(string name, bool active);
        void RemoveParticleSystem(string name);
        void SetParticleSystemActive(string name, bool active);
    }
}
