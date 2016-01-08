using SS14.Client.GameObjects;
using SS14.Client.Graphics.Sprite;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.GO;
using System;
using System.Drawing;
using SFML.Window;
using SS14.Shared.Maths;
using SS14.Client.Graphics;
using SFML.Graphics;
using Color = SFML.Graphics.Color;
using SFML.System;

namespace SS14.Client.Services.UserInterface.Components
{
    internal class StatusEffectButton : GuiComponent
    {
        private readonly IResourceManager _resourceManager;

        private readonly StatusEffect assignedEffect;

        private readonly TextSprite timeLeft;

        private readonly TextSprite tooltip;
        private Sprite _buttonSprite;

        private bool showTooltip;
        private Vector2i tooltipPos;

        public StatusEffectButton(StatusEffect _assigned, IResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
            _buttonSprite = _resourceManager.GetSprite(_assigned.icon);
            assignedEffect = _assigned;
            Color = Color.White;

            timeLeft = new TextSprite("timeleft" + _assigned.uid.ToString() + _assigned.name, "",
                                      _resourceManager.GetFont("CALIBRI"));
            timeLeft.Color = Color.White;
            timeLeft.ShadowColor = new Color(128, 128, 128);
            timeLeft.ShadowOffset = new Vector2f(1, 1);
            timeLeft.Shadowed = true;

            tooltip = new TextSprite("tooltip" + _assigned.uid.ToString() + _assigned.name, "",
                                     _resourceManager.GetFont("CALIBRI"));
            tooltip.Color = Color.Black;

            Update(0);
        }

        public Color Color { get; set; }

        public override sealed void Update(float frameTime)
        {
            _buttonSprite.Position = new Vector2f (Position.X,Position.Y);
            if (assignedEffect.doesExpire)
            {
                string leftStr = Math.Truncate(assignedEffect.expiresAt.Subtract(DateTime.Now).TotalSeconds).ToString();
                timeLeft.Text = leftStr;
                int x_pos = 16 - (int) (timeLeft.Width/2f);

                if (assignedEffect.isDebuff) timeLeft.Color = Color.Red;
                else timeLeft.Color = new Color(0, 128, 0);

                timeLeft.Position = new Vector2i(Position.X + x_pos, Position.Y + 15);
            }

            var bounds = _buttonSprite.GetLocalBounds();
            ClientArea = new IntRect(Position,
                                       new Vector2i((int)bounds.Width, (int)bounds.Height));
        }

        public override void Render()
        {
            _buttonSprite.Color = Color;
            _buttonSprite.Position =new Vector2f( Position.X, Position.Y);
            _buttonSprite.Draw();
            _buttonSprite.Color = Color.White;

            if (assignedEffect.doesExpire)
            {
                timeLeft.Draw();
            }
        }

        public void DrawTooltip() //Has to be separate so it draws on top of all buttons. 
        {
            if (showTooltip)
            {
                string leftStr = Math.Truncate(assignedEffect.expiresAt.Subtract(DateTime.Now).TotalSeconds).ToString();

                string tooltipStr = assignedEffect.name +
                                    (assignedEffect.family != StatusEffectFamily.None
                                         ? Environment.NewLine + "(" + assignedEffect.family.ToString() + ")"
                                         : "") + Environment.NewLine + Environment.NewLine +
                                    assignedEffect.description +
                                    (assignedEffect.doesExpire
                                         ? Environment.NewLine + Environment.NewLine + leftStr + " sec"
                                         : "");

                tooltip.Text = tooltipStr;
                var x_pos = (tooltipPos.X + 10 + tooltip.Width + 5) > CluwneLib.CurrentClippingViewport.Width
                                  ? 0 - tooltip.Width - 10
                                  : 10 + 5;
                tooltip.Position = new Vector2i(tooltipPos.X + x_pos + 5, tooltipPos.Y + 5 + 10);
                CluwneLib.drawRectangle(tooltipPos.X + x_pos, tooltipPos.Y + 10, tooltip.Width + 5,
                                                           tooltip.Height + 5, new Color(70, 130, 180));
                CluwneLib.drawRectangle(tooltipPos.X + x_pos, tooltipPos.Y + 10, tooltip.Width + 5,
                                                    tooltip.Height + 5, new Color(72, 61, 139));
                tooltip.Draw();
            }
        }

        public override void Dispose()
        {
            _buttonSprite = null;
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        public override bool MouseDown(MouseButtonEventArgs e)
        {
            return false;
        }

        public override bool MouseUp(MouseButtonEventArgs e)
        {
            return false;
        }

        public override void MouseMove(MouseMoveEventArgs e)
        {
            if (ClientArea.Contains(e.X, e.Y))
            {
                showTooltip = true;
                tooltipPos = new Vector2i(e.X, e.Y);
            }
            else
                showTooltip = false;
        }
    }
}