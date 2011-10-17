using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CGO
{
    public interface IRenderableComponent
    {
        void Render();
        int DrawDepth { get; set; }
        Entity Owner { get; set; }
    }
}
