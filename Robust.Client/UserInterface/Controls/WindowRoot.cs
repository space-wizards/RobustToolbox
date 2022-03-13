using Robust.Client.Graphics;

namespace Robust.Client.UserInterface.Controls
{
    public sealed class WindowRoot : UIRoot
    {
        internal WindowRoot(IClydeWindow window)
        {
            Window = window;
        }

        public override bool AutoScale => true;

        public override float UIScale => UIScaleSet;
        internal float UIScaleSet { get; set; }
        public override IClydeWindow Window { get; }
    }
}
