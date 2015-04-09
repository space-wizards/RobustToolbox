using SS14.Client.GameObjects;
using SS14.Client.Interfaces.Player;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using SS14.Shared.IoC;
using System;
using System.Drawing;
using SS14.Client.Graphics.Sprite;
using SS14.Shared.Maths;

namespace SS14.Client.Services.UserInterface.Components
{
    internal class ArmorInfoLabel : GuiComponent
    {
        private readonly IResourceManager _resourceManager;
        private readonly DamageType resAssigned;

		private CluwneSprite icon;
        private TextSprite text;

        public ArmorInfoLabel(DamageType resistance, IResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
            resAssigned = resistance;

            text = new TextSprite("StatInfoLabel" + resistance, "", _resourceManager.GetFont("CALIBRI"))
                       {Color = Color.White};

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
            icon.Position = new Vector2(Position.X,Position.Y);
            text.Position = new Vector2(Position.X + icon.Width + 5,
                                         Position.Y + (int) (icon.Height/2f) - (int) (text.Height/2f));
            ClientArea = new Rectangle(Position,
                                       new Size((int) text.Width + (int) icon.Width + 5,
                                                (int) Math.Max(icon.Height, text.Height)));

            var playerMgr = IoCManager.Resolve<IPlayerManager>();
            if (playerMgr != null)
            {
                Entity playerEnt = playerMgr.ControlledEntity;
                if (playerEnt != null)
                {
                    var statsComp = (EntityStatsComp) playerEnt.GetComponent(ComponentFamily.EntityStats);
                    if (statsComp != null)
                    {
                        text.Text = Enum.GetName(typeof (DamageType), resAssigned) + " : " +
                                    statsComp.GetArmorValue(resAssigned);
                    }
                    else text.Text = Enum.GetName(typeof (DamageType), resAssigned) + " : n/a";
                }
                else text.Text = Enum.GetName(typeof (DamageType), resAssigned) + " : n/a";
            }
            else text.Text = Enum.GetName(typeof (DamageType), resAssigned) + " : n/a";
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