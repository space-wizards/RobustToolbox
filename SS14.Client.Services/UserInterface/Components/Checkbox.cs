using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using SS14.Client.Interfaces.Resource;
using System;
using System.Drawing;

namespace SS14.Client.Services.UserInterface.Components
{
    internal class Checkbox : GuiComponent
    {
        #region Delegates

        public delegate void CheckboxChangedHandler(Boolean newValue, Checkbox sender);

        #endregion

        private readonly IResourceManager _resourceManager;

        private Sprite checkbox;
        private Sprite checkboxCheck;

        private Boolean value;

        public Checkbox(IResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
            checkbox = _resourceManager.GetSprite("checkbox0");
            checkboxCheck = _resourceManager.GetSprite("checkbox1");
            Update(0);
        }

        public Boolean Value
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
            ClientArea = new Rectangle(Position, new Size((int) checkbox.Width, (int) checkbox.Height));
        }

        public override void Render()
        {
            checkbox.Draw(ClientArea);
            if (Value) checkboxCheck.Draw(ClientArea);
        }

        public override void Dispose()
        {
            checkbox = null;
            checkboxCheck = null;
            ValueChanged = null;
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            if (ClientArea.Contains(new Point((int) e.Position.X, (int) e.Position.Y)))
            {
                Value = !Value;
                return true;
            }
            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            return false;
        }
    }
}