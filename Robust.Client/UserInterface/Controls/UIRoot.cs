using System;
using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    public abstract class UIRoot : Control
    {
        public const string StylePropBackground = "background";

        public override UIRoot? Root
        {
            get => this;
            internal set => throw new InvalidOperationException();
        }

        public new virtual IClydeWindow? Window => null;

        public Color? BackgroundColor { get; set; }

        private Color _styleBgColor;

        internal Color ActualBgColor => BackgroundColor ?? _styleBgColor;

        protected override void StylePropertiesChanged()
        {
            base.StylePropertiesChanged();

            _styleBgColor = TryGetStyleProperty(StylePropBackground, out Color color) ? color : default;
        }
    }
}
