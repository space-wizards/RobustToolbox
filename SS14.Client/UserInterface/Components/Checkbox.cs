using System;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using SS14.Client.Graphics;
using SS14.Shared.Maths;
using Vector2i = SS14.Shared.Maths.Vector2i;

namespace SS14.Client.UserInterface.Components
{
    internal class Checkbox : Control
    {
        public delegate void CheckboxChangedHandler(bool newValue, Checkbox sender);

        private Sprite _checkbox;
        private Sprite _checkboxCheck;

        private bool _value;

        public bool Value
        {
            get => _value;
            set
            {
                ValueChanged?.Invoke(value, this);
                _value = value;
            }
        }

        public Checkbox()
        {
            _checkbox = _resourceCache.GetSprite("checkbox0");
            _checkboxCheck = _resourceCache.GetSprite("checkbox1");
        }

        /// <inheritdoc />
        protected override void OnCalcRect()
        {
            var bounds = _checkbox.GetLocalBounds();
            _clientArea = Box2i.FromDimensions(new Vector2i(0, 0), new Vector2i((int) bounds.Width, (int) bounds.Height));
        }

        /// <inheritdoc />
        protected override void DrawContents()
        {
            // TODO: Move this to OnCalcPosition once the ResourceCache we stop sharing sprites between controls.
            _checkbox.Position = new Vector2f(Position.X, Position.Y);
            _checkboxCheck.Position = new Vector2f(Position.X, Position.Y);

            _checkbox.Draw();

            if (Value)
                _checkboxCheck.Draw();
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            _checkbox = null;
            _checkboxCheck = null;
            ValueChanged = null;
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        public override bool MouseDown(MouseButtonEventArgs e)
        {
            if (base.MouseDown(e))
                return true;

            if (ClientArea.Translated(Position).Contains(new Vector2i(e.X, e.Y)))
            {
                Value = !Value;
                return true;
            }
            return false;
        }

        public event CheckboxChangedHandler ValueChanged;
    }
}
