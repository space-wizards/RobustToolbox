using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.Components
{
    public class Panel : Screen
    {
        public override void Resize()
        {
            if (Background != null)
            {
                var bounds = Background.GetLocalBounds();
                _size = new Vector2i((int) bounds.Width, (int) bounds.Height);
                
            }
            base.Resize();
        }
    }
}
