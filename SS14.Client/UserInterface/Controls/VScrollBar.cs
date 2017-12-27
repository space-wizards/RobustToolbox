using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

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

        public bool IsAtBottom => Page + Value == MaxValue;
    }
}
