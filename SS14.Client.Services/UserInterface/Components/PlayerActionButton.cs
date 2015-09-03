using SS14.Client.Interfaces.GOC;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.UserInterface;
using SS14.Shared.IoC;
using System;
using SFML.Window;
using SS14.Client.Graphics.Sprite;
using System.Drawing;
using SS14.Shared.Maths;
using SS14.Client.Graphics;

namespace SS14.Client.Services.UserInterface.Components
{
    internal class PlayerActionButton : GuiComponent
    {
        private readonly IUserInterfaceManager UiMgr;
        private readonly IResourceManager _resourceManager;

        private readonly TextSprite timeLeft;

        private readonly TextSprite tooltip;
        private CluwneSprite _buttonSprite;
        public IPlayerAction assignedAction;
        private double cooldownLeft;

        private bool mouseOver;
        private DateTime mouseOverStart;
        private bool showTooltip;
        private Point tooltipPos;

        public PlayerActionButton(IPlayerAction _assigned, IResourceManager resourceManager)
        {
            UiMgr = IoCManager.Resolve<IUserInterfaceManager>();
            _resourceManager = resourceManager;

            _buttonSprite = _resourceManager.GetSprite(_assigned.Icon);
            assignedAction = _assigned;
            Color = Color.White;

            timeLeft = new TextSprite("cooldown" + _assigned.Uid.ToString() + _assigned.Name, "",
                                      _resourceManager.GetFont("CALIBRI"));
            timeLeft.Color = Color.NavajoWhite;
            timeLeft.ShadowColor = Color.Black;
            timeLeft.ShadowOffset = new Vector2(1, 1);
            timeLeft.Shadowed = true;

            tooltip = new TextSprite("tooltipAct" + _assigned.Uid.ToString() + _assigned.Name, "",
                                     _resourceManager.GetFont("CALIBRI"));
            tooltip.Color = Color.Black;

            Update(0);
        }

        public Color Color { get; set; }

        public override sealed void Update(float frameTime)
        {
            cooldownLeft = Math.Truncate(assignedAction.CooldownExpires.Subtract(DateTime.Now).TotalSeconds);

            _buttonSprite.Position = new Vector2( Position.X, Position.Y);
            if (cooldownLeft > 0)
            {
                timeLeft.Text = cooldownLeft.ToString();
                int x_pos = (int)(ClientArea.Width / 2f) - (int)(timeLeft.Width / 2f);
                int y_pos = (int)(ClientArea.Height / 2f) - (int)(timeLeft.Height / 2f);
                timeLeft.Position = new Vector2(Position.X + x_pos, Position.Y + y_pos);
            }
            else timeLeft.Text = string.Empty;

            if (mouseOver && DateTime.Now.Subtract(mouseOverStart).TotalSeconds >= 1)
                showTooltip = true;

            ClientArea = new Rectangle(Position,
                                       new Size((int)_buttonSprite.Width, (int)_buttonSprite.Height));
        }

        public override void Render()
        {
            if (cooldownLeft > 0) Color = Color.DarkGray;
            else Color = Color.White;

            _buttonSprite.Color = new SFML.Graphics.Color(Color.R,Color.G,Color.B,Color.A);
            _buttonSprite.Position = new Vector2( Position.X, Position.Y);
            _buttonSprite.Draw();
            _buttonSprite.Color = new SFML.Graphics.Color(Color.White.R,Color.White.G,Color.White.B);

            timeLeft.Draw();
        }

        public void DrawTooltip(Point offset) //Has to be separate so it draws on top of all buttons. 
        {
            if (showTooltip)
            {
                string tooltipStr = assignedAction.Name +
                                    Environment.NewLine + Environment.NewLine +
                                    assignedAction.Description +
                                    (cooldownLeft > 0
                                         ? Environment.NewLine + Environment.NewLine + "Cooldown : " +
                                           cooldownLeft.ToString() + " sec"
                                         : "");

                tooltip.Text = tooltipStr;
                float x_pos = (tooltipPos.X + 10 + tooltip.Width + 5 + offset.X) > CluwneLib.CurrentClippingViewport.Width
                                  ? 0 - tooltip.Width - 10
                                  : 10 + 5;
                tooltip.Position = new Vector2(tooltipPos.X + x_pos + 5 + offset.X, tooltipPos.Y + 5 + 10 + offset.Y);
                CluwneLib.drawRectangle((int)(tooltipPos.X + x_pos + offset.X), tooltipPos.Y + 10 + offset.Y, tooltip.Width + 5, tooltip.Height + 5, Color.SteelBlue);
                CluwneLib.drawRectangle((int)(tooltipPos.X + x_pos + offset.X), tooltipPos.Y + 10 + offset.Y, tooltip.Width + 5, (tooltip.Height + 5), Color.DarkSlateBlue);
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
            if (ClientArea.Contains(new Point((int)e.X, (int)e.Y)))
            {
                assignedAction.Activate();
                return true;
            }
            return false;
        }

        public void MouseMove(MouseButtonEventArgs e)
        {
            if (ClientArea.Contains(new Point((int)e.X, (int)e.Y)))
            {
                if (e.Button == Mouse.Button.Left)
                {
                    UiMgr.DragInfo.StartDrag(assignedAction);
                }
                else
                {
                    if (!mouseOver)
                    {
                        if (!mouseOver) mouseOverStart = DateTime.Now;
                        mouseOver = true;
                    }
                    tooltipPos = new Point((int)e.X, (int)e.Y);
                }
            }
            else
            {
                mouseOver = false;
                showTooltip = false;
            }
        }
    }
}