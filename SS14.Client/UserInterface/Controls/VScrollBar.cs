using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SS14.Shared.Log;

namespace SS14.Client.UserInterface
{
    public class VScrollBar : ScrollBar
    {
        public VScrollBar() : base()
        {
        }
        public VScrollBar(string name) : base(name)
        {
        }
        public VScrollBar(Godot.ScrollBar control) : base(control)
        {
        }

        protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.VScrollBar();
        }

        // The Value of a scrollbar is the position where the "bar" begins,
        //  so since Page is the "size" of the bar, page + value is the max.
        public bool IsAtBottom => Value + Page == MaxValue;
    }
}
