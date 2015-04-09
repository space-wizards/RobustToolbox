using SFML.Window;
using SS14.Client.GameObjects;
using SS14.Client.Graphics.CluwneLib;
using SS14.Client.Graphics.CluwneLib.Sprite;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.GO;
using SS14.Shared.Maths;
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
		private CluwneSprite _buttonSprite;

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
            timeLeft.ShadowOffset = new Vector2(1, 1);
            timeLeft.Shadowed = true;

            tooltip = new TextSprite("tooltip" + _assigned.uid.ToString() + _assigned.name, "",
                                     _resourceManager.GetFont("CALIBRI"));
            tooltip.Color = Color.Black;

            Update(0);
        }

        public Color Color { get; set; }

        public override sealed void Update(float frameTime)
        {
            _buttonSprite.Position = new Vector2 (Position.X,Position.Y);
            if (assignedEffect.doesExpire)
            {
                string leftStr = Math.Truncate(assignedEffect.expiresAt.Subtract(DateTime.Now).TotalSeconds).ToString();
                timeLeft.Text = leftStr;
                int x_pos = 16 - (int) (timeLeft.Width/2f);

                if (assignedEffect.isDebuff) timeLeft.Color = Color.Red;
                else timeLeft.Color = Color.ForestGreen;

                timeLeft.Position = new Vector2(Position.X + x_pos, Position.Y + 15);
            }

            ClientArea = new Rectangle(Position,
                                       new Size((int) _buttonSprite.AABB.Width, (int) _buttonSprite.AABB.Height));
        }

        public override void Render()
        {
            _buttonSprite.Color = new SFML.Graphics.Color(Color.R, Color.G, Color.B, Color.A);
            _buttonSprite.Position =new Vector2( Position.X, Position.Y);
            _buttonSprite.Draw();
            _buttonSprite.Color = new SFML.Graphics.Color(Color.White.R, Color.White.G, Color.White.B);

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
                float x_pos = (tooltipPos.X + 10 + tooltip.Width + 5) > CluwneLib.CurrentClippingViewport.Width
                                  ? 0 - tooltip.Width - 10
                                  : 10 + 5;
                tooltip.Position = new Vector2(tooltipPos.X + x_pos + 5, tooltipPos.Y + 5 + 10);
              CluwneLib.drawRectangle((int)(tooltipPos.X + x_pos), tooltipPos.Y + 10, tooltip.Width + 5,
                                                           tooltip.Height + 5, Color.SteelBlue);
              CluwneLib.drawRectangle((int)(tooltipPos.X + x_pos), tooltipPos.Y + 10, tooltip.Width + 5,
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
            if (ClientArea.Contains(new Point((int) e.X, (int) e.Y)))
            {
                showTooltip = true;
                tooltipPos = new Point((int) e.X, (int) e.Y);
            }
            else
                showTooltip = false;
        }
    }
}