using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameObject;

namespace ClientInterfaces.GOC
{
    public interface IParticleSystemComponent : IComponent
    {
        void AddParticleSystem(string name, bool active);
        void RemoveParticleSystem(string name);
        void SetParticleSystemActive(string name, bool active);
    }
}
