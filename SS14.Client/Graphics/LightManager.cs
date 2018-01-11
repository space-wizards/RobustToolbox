using SS14.Client.Interfaces.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Client.Graphics
{
    class LightManager : ILightManager
    {
        public bool Enabled { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void AddLightEmitter(ILightEmitter emitter)
        {
            throw new NotImplementedException();
        }

        public void RemoveLightEmitter(ILightManager emitter)
        {
            throw new NotImplementedException();
        }
    }
}
