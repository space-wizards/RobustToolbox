using System;
using System.Drawing;
using ClientInterfaces;
using ClientInterfaces.Resource;
using ClientInterfaces.GOC;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using CGO;
using GorgonLibrary;
using SS13_Shared.GO;
using ClientInterfaces.UserInterface;
using SS13.IoC;

namespace ClientServices.UserInterface.Components
{
    class PlayerActionButton : GuiComponent
    {
        private readonly IResourceManager _resourceManager;
        private Sprite _buttonSprite;

        private DateTime mouseOverStart;

        public IPlayerAction assignedAction;

        private TextSprite timeLeft;

        private TextSprite tooltip;

        public Color Color { get; set; }

        private bool mouseOver = false;
        private bool showTooltip = false;
        private Point tooltipPos = new Point();

        private IUserInterfaceManager UiMgr;

        private double cooldownLeft = 0;

        public PlayerActionButton(IPlayerAction _assigned, IResourceManager resourceManager)
        {
            UiMgr = IoCManager.Resolve<IUserInterfaceManager>();
            _resourceManager = resourceManager;

            _buttonSprite = _resourceManager.GetSprite(_assigned.Icon);
            assignedAction = _assigned;
            Color = Color.White;

            timeLeft = new TextSprite("cooldown"+_assigned.Uid.ToString()+_assigned.Name, "", _resourceManager.GetFont("CALIBRI"));
            timeLeft.Color = Color.NavajoWhite;
            timeLeft.ShadowColor = Color.Black;
            timeLeft.ShadowOffset = new Vector2D(1, 1);
            timeLeft.Shadowed = true;

            tooltip = new TextSprite("tooltipAct" + _assigned.Uid.ToString() + _assigned.Name, "", _resourceManager.GetFont("CALIBRI"));
            tooltip.Color = Color.Black;

            Update(0);
        }

        public override sealed void Update(float frameTime)
        {
            cooldownLeft = Math.Truncate(assignedAction.CooldownExpires.Subtract(DateTime.Now).TotalSeconds);

            _buttonSprite.Position = Position;
            if (cooldownLeft > 0)
            {
                timeLeft.Text = cooldownLeft.ToString();
                int x_pos = (int)(ClientArea.Width / 2f) - (int)(timeLeft.Width / 2f);
                int y_pos = (int)(ClientArea.Height / 2f) - (int)(timeLeft.Height / 2f);
                timeLeft.Position = new Vector2D(this.Position.X + x_pos, this.Position.Y + y_pos);
            }
            else timeLeft.Text = string.Empty;

            if (mouseOver && DateTime.Now.Subtract(mouseOverStart).TotalSeconds >= 1)
                showTooltip = true;

            ClientArea = new Rectangle(Position, new Size((int)_buttonSprite.AABB.Width, (int)_buttonSprite.AABB.Height));
        }

        public override void Render()
        {
            if (cooldownLeft > 0) Color = Color.DarkGray;
            else Color = Color.White;

            _buttonSprite.Color = Color;
            _buttonSprite.Position = Position;
            _buttonSprite.Draw();
            _buttonSprite.Color = Color.White;

            timeLeft.Draw();
        }

        public void DrawTooltip(Point offset) //Has to be separate so it draws on top of all buttons. 
        {
            if (showTooltip)
            {
                string tooltipStr = assignedAction.Name + 
                    Environment.NewLine + Environment.NewLine +
                    assignedAction.Description +
                    (cooldownLeft > 0 ? Environment.NewLine + Environment.NewLine + "Cooldown : " + cooldownLeft.ToString() + " sec" : "");

                tooltip.Text = tooltipStr;
                float x_pos = (tooltipPos.X + 10 + tooltip.Width + 5 + offset.X) > Gorgon.CurrentClippingViewport.Width ? 0 - tooltip.Width - 10 : 10 + 5;
                tooltip.Position = new Vector2D(tooltipPos.X + x_pos + 5 + offset.X, tooltipPos.Y + 5 + 10 + offset.Y);
                Gorgon.CurrentRenderTarget.FilledRectangle(tooltipPos.X + x_pos + offset.X, tooltipPos.Y + 10 + offset.Y, tooltip.Width + 5, tooltip.Height + 5, Color.SteelBlue);
                Gorgon.CurrentRenderTarget.Rectangle(tooltipPos.X + x_pos + offset.X, tooltipPos.Y + 10 + offset.Y, tooltip.Width + 5, tooltip.Height + 5, Color.DarkSlateBlue);
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
            if (ClientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y)))
            {
                assignedAction.Activate();
                return true;
            }
            return false;
        }

        public override void MouseMove(MouseInputEventArgs e)
        {
            if (ClientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y)))
            {
                if (e.Buttons == MouseButtons.Left)
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
                    tooltipPos = new Point((int)e.Position.X, (int)e.Position.Y);
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
