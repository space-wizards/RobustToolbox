using SFML.Graphics;
using SFML.System;
using SS14.Client.GameObjects;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Sprite;
using SS14.Client.Interfaces.Player;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using System;

namespace SS14.Client.UserInterface.Components
{
    internal class ArmorInfoLabel : GuiComponent
    {
        private readonly IResourceManager _resourceManager;
        private readonly DamageType resAssigned;

        private Sprite icon;
        private TextSprite text;

        public ArmorInfoLabel(DamageType resistance, IResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
            resAssigned = resistance;

            text = new TextSprite("StatInfoLabel" + resistance, "", _resourceManager.GetFont("CALIBRI"))
            { Color = Color.White };

            switch (resistance)
            {
                case DamageType.Bludgeoning:
                    icon = resourceManager.GetSprite("res_blunt");
                    break;
                case DamageType.Burn:
                    icon = resourceManager.GetSprite("res_burn");
                    break;
                case DamageType.Freeze:
                    icon = resourceManager.GetSprite("res_freeze");
                    break;
                case DamageType.Piercing:
                    icon = resourceManager.GetSprite("res_pierce");
                    break;
                case DamageType.Shock:
                    icon = resourceManager.GetSprite("res_shock");
                    break;
                case DamageType.Slashing:
                    icon = resourceManager.GetSprite("res_slash");
                    break;
                case DamageType.Suffocation:
                    icon = resourceManager.GetSprite("statusbar_switch");
                    break;
                case DamageType.Toxin:
                    icon = resourceManager.GetSprite("res_tox");
                    break;
                case DamageType.Untyped:
                    icon = resourceManager.GetSprite("statusbar_switch");
                    break;
            }

            Update(0);
        }

        public override void Update(float frameTime)
        {
            var bounds = icon.GetLocalBounds();
            icon.Position = new Vector2f(Position.X, Position.Y);
            text.Position = new Vector2i(Position.X + (int)bounds.Width + 5,
                                         Position.Y + (int)(bounds.Height / 2f) - (int)(text.Height / 2f));
            ClientArea = new IntRect(Position,
                                       new Vector2i((int)text.Width + (int)bounds.Width + 5,
                                                (int)Math.Max(bounds.Height, text.Height)));

            var playerMgr = IoCManager.Resolve<IPlayerManager>();
            if (playerMgr != null)
            {
                IEntity playerEnt = playerMgr.ControlledEntity;
                if (playerEnt != null)
                {
                    var statsComp = (EntityStatsComp)playerEnt.GetComponent(ComponentFamily.EntityStats);
                    if (statsComp != null)
                    {
                        text.Text = Enum.GetName(typeof(DamageType), resAssigned) + " : " +
                                    statsComp.GetArmorValue(resAssigned);
                    }
                    else text.Text = Enum.GetName(typeof(DamageType), resAssigned) + " : n/a";
                }
                else text.Text = Enum.GetName(typeof(DamageType), resAssigned) + " : n/a";
            }
            else text.Text = Enum.GetName(typeof(DamageType), resAssigned) + " : n/a";
        }

        public override void Render()
        {
            icon.Draw();
            text.Draw();
        }

        public override void Dispose()
        {
            text = null;
            icon = null;
            base.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
