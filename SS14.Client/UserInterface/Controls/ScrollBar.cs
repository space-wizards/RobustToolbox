using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Client.UserInterface
{
    public abstract class ScrollBar : Range
    {
        public ScrollBar() : base()
        {
        }
        public ScrollBar(string name) : base(name)
        {
        }
        public ScrollBar(Godot.ScrollBar control) : base(control)
        {
        }
    }
}
