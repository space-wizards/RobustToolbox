using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using ClientInterfaces.GOC;
using ClientInterfaces.Lighting;
using ClientInterfaces.Map;
using GorgonLibrary;
using SS13_Shared;
using SS13_Shared.GO;

namespace ClientInterfaces.Lighting
{
    public interface LightMode
    {
        LightModeClass LightModeClass { get; set; }
        void OnAdd(ILight owner);
        void OnRemove(ILight owner);
        void Update(ILight owner, float frametime);
    }
}
