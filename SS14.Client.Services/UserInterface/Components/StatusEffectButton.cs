using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using SS14.Client.GameObjects;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.GO;
using System;
using System.Drawing;

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
        private Point tooltipPos;

        public StatusEffectButton(StatusEffect _assigned, IResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
            _buttonSprite = _resourceManager.GetSprite(_assigned.icon);
            assignedEffect = _assigned;
            Color = Color.White;

            timeLeft = new TextSprite("timeleft" + _assigned.uid.ToString() + _assigned.name, "",
                                      _resourceManager.GetFont("CALIBRI"));
            timeLeft.Color = Color.White;
            timeLeft.ShadowColor = Color.Gray;
            timeLeft.ShadowOffset = new Vector2D(1, 1);
            timeLeft.Shadowed = true;

            tooltip = new TextSprite("tooltip" + _assigned.uid.ToString() + _assigned.name, "",
                                     _resourceManager.GetFont("CALIBRI"));
            tooltip.Color = Color.Black;

            Update(0);
        }

        public Color Color { get; set; }

        public override sealed void Update(float frameTime)
        {
            _buttonSprite.Position = Position;
            if (assignedEffect.doesExpire)
            {
                string leftStr = Math.Truncate(assignedEffect.expiresAt.Subtract(DateTime.Now).TotalSeconds).ToString();
                timeLeft.Text = leftStr;
                int x_pos = 16 - (int) (timeLeft.Width/2f);

                if (assignedEffect.isDebuff) timeLeft.Color = Color.Red;
                else timeLeft.Color = Color.ForestGreen;

                timeLeft.Position = new Vector2D(Position.X + x_pos, Position.Y + 15);
            }

            ClientArea = new Rectangle(Position,
                                       new Size((int) _buttonSprite.AABB.Width, (int) _buttonSprite.AABB.Height));
        }

        public override void Render()
        {
            _buttonSprite.Color = Color;
            _buttonSprite.Position = Position;
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
                float x_pos = (tooltipPos.X + 10 + tooltip.Width + 5) > Gorgon.CurrentClippingViewport.Width
                                  ? 0 - tooltip.Width - 10
                                  : 10 + 5;
                tooltip.Position = new Vector2D(tooltipPos.X + x_pos + 5, tooltipPos.Y + 5 + 10);
                Gorgon.CurrentRenderTarget.FilledRectangle(tooltipPos.X + x_pos, tooltipPos.Y + 10, tooltip.Width + 5,
                                                           tooltip.Height + 5, Color.SteelBlue);
                Gorgon.CurrentRenderTarget.Rectangle(tooltipPos.X + x_pos, tooltipPos.Y + 10, tooltip.Width + 5,
                                                     tooltip.Height + 5, Color.DarkSlateBlue);
                tooltip.Draw();
            }
        }

        public override void Dispose()
        {
            _buttonSprite = null;
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            return false;
        }

        public override void MouseMove(MouseInputEventArgs e)
        {
            if (ClientArea.Contains(new Point((int) e.Position.X, (int) e.Position.Y)))
            {
                showTooltip = true;
                tooltipPos = new Point((int) e.Position.X, (int) e.Position.Y);
            }
            else
                showTooltip = false;
        }
    }
}