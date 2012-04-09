using System;
using System.Drawing;
using CGO;
using ClientInterfaces;
using ClientInterfaces.Player;
using ClientInterfaces.Resource;
using ClientInterfaces.UserInterface;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using SS13_Shared.GO;
using SS13_Shared;
using SS13.IoC;
using GorgonLibrary.InputDevices;
using ClientServices.Helpers;

namespace ClientServices.UserInterface.Components
{
    public class HealthPanel : GuiComponent
    {
        Rectangle healthMeterInner = new Rectangle();

        Sprite healthMeterBg;
        Sprite healthMeterOverlay;
        Sprite healthMeterGrid;
        Sprite panelBG;

        Label healthPc;

        IPlayerManager _playerManager = IoCManager.Resolve<IPlayerManager>();
        IUserInterfaceManager _userInterfaceManager = IoCManager.Resolve<IUserInterfaceManager>();
        IResourceManager _resMgr = IoCManager.Resolve<IResourceManager>();
        Sprite _backgroundSprite;

        private readonly Color ColHealthy = Color.FromArgb(11, 83, 2);
        private readonly Color ColCritical = Color.FromArgb(83, 19, 2);

        Color interpCol = Color.Transparent;

        private float blipArea = 0;
        private readonly int blipAreaWidth = 15;

        private float healthPct = 0;

        public HealthPanel()
            : base()
        {

            ComponentClass = GuiComponentType.Undefined;

            healthMeterBg = _resMgr.GetSprite("healthMeterBg");
            healthMeterOverlay = _resMgr.GetSprite("healthMeterOverlay");
            healthMeterGrid = _resMgr.GetSprite("healthMeterGrid");
            panelBG = _resMgr.GetSprite("healthBg");
            _backgroundSprite = _resMgr.GetSprite("1pxwhite");

            healthPc = new Label("100", "CALIBRI", _resMgr);
            healthPc.Text.ShadowOffset = new Vector2D(1, 1);
            healthPc.Text.Shadowed = true;
            healthPc.Text.Color = Color.FloralWhite;
        }

        public override void ComponentUpdate(params object[] args)
        {
        }

        public override void Update()
        {
            const int x_inner = 4;
            const int y_inner = 25;
            const int dec_inner = 7;

            panelBG.Position = Position;
            healthMeterBg.Position = new Vector2D(Position.X + x_inner, Position.Y + y_inner);
            healthMeterOverlay.Position = new Vector2D(Position.X + x_inner, Position.Y + y_inner);
            healthMeterGrid.Position = new Vector2D(Position.X + x_inner, Position.Y + y_inner);
            ClientArea = Rectangle.Round(panelBG.AABB);
            healthMeterInner = new Rectangle(Position.X + x_inner + dec_inner, Position.Y + y_inner + dec_inner, (int)(healthMeterOverlay.Width - (2 * dec_inner)), (int)(healthMeterOverlay.Height - (2 * dec_inner)));
            healthPc.Position = new Point(healthMeterInner.X + 5, (int)(healthMeterInner.Y + (healthMeterInner.Height / 2f) - (healthPc.ClientArea.Height / 2f)) - 2);
            healthPc.Update();

            var entity = _playerManager.ControlledEntity;

            if (entity != null && entity.HasComponent(ComponentFamily.Damageable))
            {
                var comp = (HealthComponent)entity.GetComponent(ComponentFamily.Damageable);
                float _health = comp.GetHealth();

                healthPct = comp.GetHealth() / comp.GetMaxHealth();
                if (float.IsNaN(healthPct)) healthPct = 1; //This can happen when the components are not ready yet.

                interpCol = ColorInterpolator.InterpolateBetween(ColCritical, ColHealthy, healthPct);
                healthPc.Text.Text = Math.Round((healthPct * 100)).ToString() + "%";
            }
        }

        public override void Render()
        {
            panelBG.Draw();
            healthMeterBg.Draw();
            Gorgon.CurrentRenderTarget.FilledRectangle(healthMeterInner.X, healthMeterInner.Y, healthMeterInner.Width, healthMeterInner.Height, interpCol);
            healthPc.Render();
            healthMeterGrid.Draw();
            RenderBlip();
            healthMeterOverlay.Draw();
        }

        private void RenderBlip()
        {
            int x_off = 25;

            int half = (int)(healthMeterInner.Width / 2.5f);

            float blipCount = healthMeterInner.X + x_off + blipArea;

            Gorgon.CurrentRenderTarget.BlendingMode = BlendingModes.Additive;
            while (blipCount <= (healthMeterInner.X + x_off + blipArea) + blipAreaWidth)
            {
                float blipHeight = Math.Abs((half + healthMeterInner.X + x_off) - blipCount) * healthPct;
                float alpha = Math.Max(Math.Min((half / blipHeight) * 15, 255) * Math.Max(healthPct, 0.10f),0);
                _backgroundSprite.Color = Color.FromArgb((int)alpha, Color.FloralWhite);
                _backgroundSprite.Draw(new Rectangle((int)blipCount, (int)Math.Min(healthMeterInner.Y + blipHeight, (blipCount >= healthMeterInner.X + x_off + half ? healthMeterInner.Y + healthMeterInner.Height / 2f : healthMeterInner.Y + healthMeterInner.Height / 3f)) + 5 + (int)(10 - (10 * healthPct)), 2, 2));
                blipCount++;
            }
            Gorgon.CurrentRenderTarget.BlendingMode = BlendingModes.None;

            if ((blipArea + 1.3f) >= 100) blipArea = 0;
            else blipArea += 1.3f;
        }

        public override void Resize()
        {
        }

        public override void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public override void HandleNetworkMessage(Lidgren.Network.NetIncomingMessage message)
        {
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            if (ClientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y)))
            {
                return true;
            }
            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            if (ClientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y)))
            {
                return true;
            }
            return false;
        }

        public override void MouseMove(MouseInputEventArgs e)
        {
        }

        public override bool MouseWheelMove(MouseInputEventArgs e)
        {
            return false;
        }

        public override bool KeyDown(KeyboardInputEventArgs e)
        {
            return false;
        }
    }
}
