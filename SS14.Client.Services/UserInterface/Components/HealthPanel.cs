using SS14.Client.Graphics.CluwneLib;
using SS14.Client.Graphics.CluwneLib.Sprite;
using Lidgren.Network;
using SS14.Client.GameObjects;
using SS14.Client.Interfaces.Player;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.Services.Helpers;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using SS14.Shared.IoC;
using System;
using System.Drawing;
using SFML.Window;

namespace SS14.Client.Services.UserInterface.Components
{
    public class HealthPanel : GuiComponent
    {
        private readonly Color ColCritical = Color.FromArgb(83, 19, 2);
        private readonly Color ColHealthy = Color.FromArgb(11, 83, 2);
		private readonly CluwneSprite _backgroundSprite;
        private readonly IPlayerManager _playerManager = IoCManager.Resolve<IPlayerManager>();
        private readonly IResourceManager _resMgr = IoCManager.Resolve<IResourceManager>();
		private readonly CluwneSprite healthMeterBg;
		private readonly CluwneSprite healthMeterGrid;
		private readonly CluwneSprite healthMeterOverlay;

        private readonly Label healthPc;
		private readonly CluwneSprite panelBG;

        private IUserInterfaceManager _userInterfaceManager = IoCManager.Resolve<IUserInterfaceManager>();
        private int blipSpeed = 60;
        private double blipTime;
        private int blipWidth = 20;
        private Rectangle healthMeterInner;

        private float healthPct;
        private Color interpCol = Color.Transparent;

        public HealthPanel()
        {
            ComponentClass = GuiComponentType.Undefined;

            healthMeterBg = _resMgr.GetSprite("healthMeterBg");
            healthMeterOverlay = _resMgr.GetSprite("healthMeterOverlay");
            healthMeterGrid = _resMgr.GetSprite("healthMeterGrid");
            panelBG = _resMgr.GetSprite("healthBg");
            _backgroundSprite = _resMgr.GetSprite("blip");

            healthPc = new Label("100", "CALIBRI", _resMgr);
            healthPc.Text.ShadowOffset = new Vector2(1, 1);
            healthPc.Text.Shadowed = true;
            healthPc.Text.Color = Color.FloralWhite;
        }

        public override void ComponentUpdate(params object[] args)
        {
        }

        public override void Update(float frameTime)
        {
            const int x_inner = 4;
            const int y_inner = 25;
            const int dec_inner = 7;

            panelBG.Position = Position;
            healthMeterBg.Position = new Vector2(Position.X + x_inner, Position.Y + y_inner);
            healthMeterOverlay.Position = new Vector2(Position.X + x_inner, Position.Y + y_inner);
            healthMeterGrid.Position = new Vector2(Position.X + x_inner, Position.Y + y_inner);

            ClientArea = Rectangle.Round(panelBG.AABB);

            healthMeterInner = new Rectangle(Position.X + x_inner + dec_inner, Position.Y + y_inner + dec_inner,
                                             (int) (healthMeterOverlay.Width - (2*dec_inner)),
                                             (int) (healthMeterOverlay.Height - (2*dec_inner)));
            healthPc.Position = new Point(healthMeterInner.X + 5,
                                          (int)
                                          (healthMeterInner.Y + (healthMeterInner.Height/2f) -
                                           (healthPc.ClientArea.Height/2f)) - 2);
            healthPc.Update(frameTime);

            Entity entity = _playerManager.ControlledEntity;

            if (entity != null && entity.HasComponent(ComponentFamily.Damageable))
            {
                var comp = (HealthComponent) entity.GetComponent(ComponentFamily.Damageable);
                float _health = comp.GetHealth();

                healthPct = comp.GetHealth()/comp.GetMaxHealth();
                if (float.IsNaN(healthPct)) healthPct = 1; //This can happen when the components are not ready yet.

                interpCol = ColorInterpolator.InterpolateBetween(ColCritical, ColHealthy, healthPct);
                healthPc.Text.Text = Math.Round((healthPct*100)).ToString() + "%";
            }

            blipTime += frameTime;
        }

        public override void Render()
        {
            panelBG.Draw();
            healthMeterBg.Draw();
            Gorgon.CurrentRenderTarget.FilledRectangle(healthMeterInner.X, healthMeterInner.Y, healthMeterInner.Width,
                                                       healthMeterInner.Height, interpCol);
            healthPc.Render();
            healthMeterGrid.Draw();
            RenderBlip();
            healthMeterOverlay.Draw();
        }

        private void RenderBlip()
        {
            int x_off = 38;
            int y_off = 17;

            int blipMaxArea = 90;

            int blipUp = 45;
            int blipDown = 57;

            if (blipTime*blipSpeed > blipMaxArea)
                blipTime = 0;

            var bs = (int) Math.Floor(blipTime*blipSpeed);

            Gorgon.CurrentRenderTarget.BlendingMode = BlendingModes.Modulated;
            for (int i = bs; i < (bs + blipWidth); i++)
            {
                float sweepPct = (float) i/(bs + blipWidth);

                float alpha =
                    Math.Min(Math.Max((1 - (Math.Abs((blipMaxArea/2f) - i)/(blipMaxArea/2f)))*(300f*sweepPct), 0f), 255f);
                _backgroundSprite.Color = Color.FromArgb((int) alpha,
                                                         ColorInterpolator.InterpolateBetween(Color.Orange,
                                                                                              Color.LawnGreen, healthPct));

                float blipHeightUp = Math.Max(((blipUp - Math.Abs(blipUp - i))/(float) blipUp) - 0.80f, 0f);
                float blipHeightDown = Math.Max(((blipDown - Math.Abs(blipDown - i))/(float) blipDown) - 0.93f, 0f);

                if (i <= blipMaxArea)
                    _backgroundSprite.Draw(new Rectangle(healthMeterInner.X + x_off + i,
                                                         healthMeterInner.Y + y_off -
                                                         (int)
                                                         ((blipHeightUp*65)*
                                                          ((healthPct > 0f) ? Math.Max(healthPct, 0.30f) : 0)) +
                                                         (int)
                                                         ((blipHeightDown*65)*
                                                          ((healthPct > 0f) ? Math.Max(healthPct, 0.45f) : 0)),
                                                         3, 3));
            }
            Gorgon.CurrentRenderTarget.BlendingMode = BlendingModes.None;
        }

        public override void Resize()
        {
        }

        public override void Dispose()
        {
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        public override void HandleNetworkMessage(NetIncomingMessage message)
        {
        }

		public override bool MouseDown(MouseButtonEventArgs e)
        {
            if (ClientArea.Contains(new Point((int) e.Position.X, (int) e.Position.Y)))
            {
                return true;
            }
            return false;
        }

		public override bool MouseUp(MouseButtonEventArgs e)
        {
            if (ClientArea.Contains(new Point((int) e.Position.X, (int) e.Position.Y)))
            {
                return true;
            }
            return false;
        }

		public override void MouseMove(MouseMoveEventArgs e)
        {
        }

		public override bool MouseWheelMove(MouseWheelEventArgs e)
        {
            return false;
        }

		public override bool KeyDown(KeyEventArgs e)
        {
            return false;
        }
    }
}