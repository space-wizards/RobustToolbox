using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using GorgonLibrary.GUI;

using Lidgren.Network;
using SS3D_shared;

namespace SS3D.Modules.UI.Components
{
    class Checkbox : GuiComponent
    {
        GUIElement checkbox;
        GUIElement checkboxCheck;

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

        public Checkbox() 
            : base()
        {
            checkbox = UiManager.Singleton.Skin.Elements["Controls.CheckBox.Box"];
            checkboxCheck = UiManager.Singleton.Skin.Elements["Controls.CheckBox.Check"];
            Update();
        }

        public override void Update()
        {
            clientArea = new Rectangle(this.position, new Size(checkbox.Dimensions.Width, checkbox.Dimensions.Height));
        }

        public override void Render()
        {
            checkbox.Draw(clientArea);
            if (Value) checkboxCheck.Draw(clientArea);
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
            if (clientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y)))
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
