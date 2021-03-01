using Robust.Client.Graphics;

namespace Robust.Client.UserInterface.Controls
{
    public sealed class WindowRoot : UIRoot
    {
        internal WindowRoot(IClydeWindow window)
        {
            Window = window;
        }

        public IClydeWindow Window { get; }
    }
}
