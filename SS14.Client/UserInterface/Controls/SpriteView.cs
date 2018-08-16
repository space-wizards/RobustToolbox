using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Shared.Log;

namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(GodotGlue.SpriteView))]
    public class SpriteView : Control
    {
        ISpriteProxy Mirror;
        ISpriteComponent _sprite;
        public ISpriteComponent Sprite
        {
            get => _sprite;
            set
            {
                _sprite = value;
                if (Mirror != null)
                {
                    Mirror.Dispose();
                    Mirror = null;
                }

                if (value != null)
                {
                    Mirror = value.CreateProxy();
                    Mirror.AttachToControl(this);
                    UpdateMirrorPosition();
                }
            }
        }

        public SpriteView() : base()
        {

        }

        public SpriteView(string name) : base(name)
        {

        }

        public SpriteView(GodotGlue.SpriteView control) : base(control)
        {

        }

        protected override void Resized()
        {
            base.Resized();
            UpdateMirrorPosition();
        }

        void UpdateMirrorPosition()
        {
            if (Mirror == null)
            {
                return;
            }

            Mirror.Offset = Size / 2;
            Logger.Fatal($"{Mirror.Offset}\n{System.Environment.StackTrace}");
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            Mirror?.Dispose();
        }
    }
}
