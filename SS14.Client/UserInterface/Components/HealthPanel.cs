using Lidgren.Network;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using SS14.Client.GameObjects;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.Player;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.UserInterface;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using System;

namespace SS14.Client.UserInterface.Components
{
    public class HealthPanel : GuiComponent
    {
        private readonly Color ColCritical = new Color(83, 19, 2);
        private readonly Color ColHealthy = new Color(11, 83, 2);
        private readonly Sprite _backgroundSprite;
        private readonly IPlayerManager _playerManager = IoCManager.Resolve<IPlayerManager>();
        private readonly IResourceManager _resMgr = IoCManager.Resolve<IResourceManager>();
        private readonly Sprite healthMeterBg;
        private readonly Sprite healthMeterGrid;
        private readonly Sprite healthMeterOverlay;

        private readonly Label healthPc;
        private readonly Sprite panelBG;

        private IUserInterfaceManager _userInterfaceManager = IoCManager.Resolve<IUserInterfaceManager>();
        private int blipSpeed = 60;
        private double blipTime;
        private int blipWidth = 20;
        private IntRect healthMeterInner;

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
            healthPc.Text.ShadowOffset = new Vector2f(1, 1);
            healthPc.Text.Shadowed = true;
            healthPc.Text.Color = new SFML.Graphics.Color(255, 250, 240);
        }

        public override void ComponentUpdate(params object[] args)
        {
        }

        public override void Update(float frameTime)
        {
            const int x_inner = 4;
            const int y_inner = 25;
            const int dec_inner = 7;

            panelBG.Position =new Vector2f (Position.X, Position.Y);
            healthMeterBg.Position = new Vector2f(Position.X + x_inner, Position.Y + y_inner);
            healthMeterOverlay.Position = new Vector2f(Position.X + x_inner, Position.Y + y_inner);
            healthMeterGrid.Position = new Vector2f(Position.X + x_inner, Position.Y + y_inner);

            var panelBounds = panelBG.GetLocalBounds();
            ClientArea = new FloatRect(panelBounds.Left, panelBounds.Top, panelBounds.Width, panelBounds.Height).Round();

            var healthMeterBounds = healthMeterOverlay.GetLocalBounds();

            healthMeterInner = new IntRect(Position.X + x_inner + dec_inner, Position.Y + y_inner + dec_inner,
                                             (int) (healthMeterBounds.Width - (2*dec_inner)),
                                             (int) (healthMeterBounds.Height - (2*dec_inner)));
            healthPc.Position = new Vector2i(healthMeterInner.Left + 5,
                                          (int)
                                          (healthMeterInner.Top + (healthMeterInner.Height/2f) -
                                           (healthPc.ClientArea.Height/2f)) - 2);
            healthPc.Update(frameTime);

            Entity entity = _playerManager.ControlledEntity;

            if (entity != null && entity.HasComponent(ComponentFamily.Damageable))
            {
                var comp = (HealthComponent) entity.GetComponent(ComponentFamily.Damageable);
                float _health = comp.GetHealth();

                healthPct = comp.GetHealth()/comp.GetMaxHealth();
                if (float.IsNaN(healthPct)) healthPct = 1; //This can happen when the components are not ready yet.

                interpCol = ColorUtils.InterpolateBetween(ColCritical, ColHealthy, healthPct);
                healthPc.Text.Text = Math.Round((healthPct*100)).ToString() + "%";
            }

            blipTime += frameTime;
        }

        public override void Render()
        {
            panelBG.Draw();
            healthMeterBg.Draw();
            CluwneLib.drawRectangle(healthMeterInner.Left, healthMeterInner.Top, healthMeterInner.Width,  healthMeterInner.Height, interpCol);
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

            CluwneLib.BlendingMode = BlendingModes.Modulated;
            for (int i = bs; i < (bs + blipWidth); i++)
            {
                float sweepPct = (float) i/(bs + blipWidth);

                float alpha =
                    Math.Min(Math.Max((1 - (Math.Abs((blipMaxArea/2f) - i)/(blipMaxArea/2f)))*(300f*sweepPct), 0f), 255f);
               Color temp = ColorUtils.InterpolateBetween(new Color(255, 165, 0, (byte)alpha),  new Color(124, 252, 0, (byte)alpha), healthPct);
               _backgroundSprite.Color = temp;

                float blipHeightUp = Math.Max(((blipUp - Math.Abs(blipUp - i))/(float) blipUp) - 0.80f, 0f);
                float blipHeightDown = Math.Max(((blipDown - Math.Abs(blipDown - i))/(float) blipDown) - 0.93f, 0f);

                if (i <= blipMaxArea)
                {
                    _backgroundSprite.SetTransformToRect(new IntRect(healthMeterInner.Left + x_off + i,
                                                         healthMeterInner.Top + y_off -
                                                         (int)((blipHeightUp * 65) * ((healthPct > 0f) ? Math.Max(healthPct, 0.30f) : 0)) +
                                                         (int)((blipHeightDown * 65) * ((healthPct > 0f) ? Math.Max(healthPct, 0.45f) : 0)),
                                                         3, 3));
                    _backgroundSprite.Draw();
                }
            }
           CluwneLib.BlendingMode = BlendingModes.None;
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
            if (ClientArea.Contains(e.X, e.Y))
            {
                return true;
            }
            return false;
        }

        public override bool MouseUp(MouseButtonEventArgs e)
        {
            if (ClientArea.Contains(e.X, e.Y))
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
