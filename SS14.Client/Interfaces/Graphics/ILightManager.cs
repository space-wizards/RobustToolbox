using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Client.Interfaces.Graphics
{
    interface ILightManager
    {
        bool Enabled { get; set; }

        void AddLightEmitter(ILightEmitter emitter);
        void RemoveLightEmitter(ILightManager emitter);
    }
}
