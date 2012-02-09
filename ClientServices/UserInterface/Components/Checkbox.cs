using System;
using System.Drawing;
using ClientInterfaces;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;

namespace ClientServices.UserInterface.Components
{
    class Checkbox : GuiComponent
    {
        private readonly IResourceManager _resourceManager;

        Sprite checkbox;
        Sprite checkboxCheck;

        private Boolean value = false;
        public Boolean Value
        {
            get
            {
                return value;
            }
            set
            {
                if (ValueChanged != null) ValueChanged(value);
                this.value = value;
            }
        }

        public delegate void CheckboxChangedHandler(Boolean newValue);
        public event CheckboxChangedHandler ValueChanged;

        public Checkbox(IResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
            checkbox = _resourceManager.GetSprite("nosprite");
            checkboxCheck = _resourceManager.GetSprite("nosprite");
            Update();
        }

        public override void Update()
        {
            ClientArea = new Rectangle(this.Position, new Size((int)checkbox.Width, (int)checkbox.Height));
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
            if (ClientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y)))
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
