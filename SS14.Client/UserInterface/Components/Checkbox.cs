using SFML.Graphics;
using SFML.Window;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.Resource;
using System;

namespace SS14.Client.UserInterface.Components
{
    internal class Checkbox : GuiComponent
    {
        #region Delegates

        public delegate void CheckboxChangedHandler(Boolean newValue, Checkbox sender);

        #endregion

        private readonly IResourceCache _resourceCache;

        private Sprite checkbox;
        private Sprite checkboxCheck;

        private bool value;


        public Checkbox(IResourceCache resourceCache)
        {
            _resourceCache = resourceCache;
            checkbox = _resourceCache.GetSprite("checkbox0");
            checkboxCheck = _resourceCache.GetSprite("checkbox1");

            ClientArea = new IntRect(Position, 
                new SFML.System.Vector2i((int)checkbox.GetLocalBounds().Width, (int)checkbox.GetLocalBounds().Height));
            Update(0);

        }

        public bool Value
        {
            get { return value; }
            set
            {
                if (ValueChanged != null) ValueChanged(value, this);
                this.value = value;
            }
        }

        public event CheckboxChangedHandler ValueChanged;

        public override void Update(float frameTime)
        {
            checkbox.Position = new SFML.System.Vector2f(Position.X, Position.Y);
        }

        public override void Render()
        {           
            checkbox.Draw();
            if (Value) checkboxCheck.Draw();
        }

        public override void Dispose()
        {
            checkbox = null;
            checkboxCheck = null;
            ValueChanged = null;
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        public override bool MouseDown(MouseButtonEventArgs e)
        {
            if (ClientArea.Contains(e.X, e.Y))
            {
                Value = !Value;
                return true;
            }
            return false;
        }

        public override bool MouseUp(MouseButtonEventArgs e)
        {
            return false;
        }
    }
}