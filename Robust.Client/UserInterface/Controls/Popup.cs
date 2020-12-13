using System;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Shared.Maths;
using Robust.Shared.IoC;
using Robust.Shared.Interfaces.GameObjects;

namespace Robust.Client.UserInterface.Controls
{
    public class Popup : Control
    {
        private readonly IEyeManager _eyeManager = default!;

        public Popup()
        {
            Visible = false;
            _eyeManager = IoCManager.Resolve<IEyeManager>();
        }

        public event Action? OnPopupHide;
        private Vector2 _desiredSize;

        public void Open(UIBox2? box = null, Vector2? altPos = null, IEntity? targetEntity = null)
        {
            if (Visible)
            {
                UserInterfaceManagerInternal.RemoveModal(this);
            }

            if (box != null && _desiredSize != box.Value.Size)
            {
                PopupContainer.SetPopupOrigin(this, box.Value.TopLeft);
                PopupContainer.SetAltOrigin(this, altPos);

                _desiredSize = box.Value.Size;
                MinimumSizeChanged();
            }

            Visible = true;
            UserInterfaceManagerInternal.PushModal(this);
        }

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            PopupContainer.GetTargetEntityProperty(this, out var targetEntity);
            if (targetEntity != null && targetEntity.Transform != null && _eyeManager != null)
            {
                // draw points are in the middle of the thick line
                var drawTo = _eyeManager.WorldToScreen(targetEntity.Transform.MapPosition.Position) - this.Position;
                // clamp the origin to the popup edges/corners
                var drawFrom = Vector2.Clamp(drawTo, new Vector2(1, 1), CombinedMinimumSize - 1);

                var vertices = new Vector2[4];
                var width = 4f;

                // we will pivot the direction of the line 90 degrees in opposite directions from the origin and destination so get 4 corners
                var pivot = new Angle(Math.PI / 2f);
                vertices[0] = drawFrom - pivot.RotateVec((drawFrom - drawTo).Normalized) * width / 2f;
                vertices[1] = drawFrom + pivot.RotateVec((drawFrom - drawTo).Normalized) * width / 2f;
                vertices[2] = drawTo + pivot.RotateVec((drawFrom - drawTo).Normalized) * width / 2f;
                vertices[3] = drawTo - pivot.RotateVec((drawFrom - drawTo).Normalized) * width / 2f;

                handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, vertices, new Color(80, 160, 20, 180));
            }
        }

        protected internal override void ModalRemoved()
        {
            base.ModalRemoved();

            Visible = false;
            OnPopupHide?.Invoke();
        }

        protected override Vector2 CalculateMinimumSize()
        {
            return Vector2.ComponentMax(_desiredSize, base.CalculateMinimumSize());
        }
    }
}
