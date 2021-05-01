using System;
using Robust.Client.Graphics;

namespace Robust.Client.UserInterface.Controls
{
    public abstract class UIRoot : Control
    {
        public override UIRoot? Root
        {
            get => this;
            internal set => throw new InvalidOperationException();
        }

        public new virtual IClydeWindow? Window => null;
    }
}
