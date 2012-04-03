using System;
using System.Drawing;
using ClientInterfaces;
using ClientInterfaces.Resource;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using CGO;
using SS13_Shared.GO;
using SS13.IoC;
using ClientInterfaces.Player;
using ClientInterfaces.GOC;

namespace ClientServices.UserInterface.Components
{
    class ArmorInfoLabel : GuiComponent
    {
        private readonly IResourceManager _resourceManager;
        private DamageType resAssigned;

        private TextSprite text;
        private Sprite icon;

        public ArmorInfoLabel(DamageType resistance, IResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
            resAssigned = resistance;

            text = new TextSprite("StatInfoLabel" + resistance, "", _resourceManager.GetFont("CALIBRI")) { Color = Color.White };

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

            Update();
        }

        public override void Update()
        {
            icon.Position = Position;
            text.Position = new Vector2D(Position.X + icon.Width + 5, Position.Y + (int)(icon.Height / 2f) - (int)(text.Height / 2f));
            ClientArea = new Rectangle(Position, new Size((int)text.Width + (int)icon.Width + 5, (int)Math.Max(icon.Height, text.Height)));

            IPlayerManager playerMgr = IoCManager.Resolve<IPlayerManager>();
            if (playerMgr != null)
            {
                IEntity playerEnt = playerMgr.ControlledEntity;
                if (playerEnt != null)
                {
                    EntityStatsComp statsComp = (EntityStatsComp)playerEnt.GetComponent(ComponentFamily.EntityStats);
                    if (statsComp != null)
                    {
                        text.Text = Enum.GetName(typeof(DamageType), resAssigned) + " : " + statsComp.GetArmorValue(resAssigned);
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
