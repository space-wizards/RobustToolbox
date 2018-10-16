using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap("ScrollBar")]
    public abstract class ScrollBar : Range
    {
        public ScrollBar() : base()
        {
        }
        public ScrollBar(string name) : base(name)
        {
        }
        #if GODOT
        public ScrollBar(Godot.ScrollBar control) : base(control)
        {
        }
        #endif
    }
}
