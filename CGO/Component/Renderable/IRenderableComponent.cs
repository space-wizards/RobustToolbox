using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CGO
{
    public interface IRenderableComponent
    {
        void Render();
        SS13_Shared.GO.DrawDepth DrawDepth { get; set; }
        Entity Owner { get; set; }
    }
}
