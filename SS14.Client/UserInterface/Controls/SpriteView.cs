using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Shared.Log;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.Controls
{
    #if GODOT
    [ControlWrap(typeof(GodotGlue.SpriteView))]
    #endif
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

        #if GODOT
        public SpriteView(GodotGlue.SpriteView control) : base(control)
        {

        }
        #endif

        protected override void Initialize()
        {
            base.Initialize();

            RectClipContent = true;
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
        }

        protected override Vector2 CalculateMinimumSize()
        {
            // TODO: make this not hardcoded.
            // It'll break on larger things.
            return new Vector2(32, 32);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            Mirror?.Dispose();
        }
    }
}
