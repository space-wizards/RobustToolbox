using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Reflection;

using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;

using Lidgren.Network;

using SS3D_shared;

namespace SS3D.Modules.UI.Components
{
    public class HumanHandsGui : GuiComponent
    {
        public override Point Position
        {
            get
            {
                return position;
            }
            set
            {
                position = value;
                lHandSprite.Position = position;
                lHandActiveSprite.Position = position;
                rHandSprite.Position = new Point(position.X + 76, position.Y);
                rHandActiveSprite.Position = rHandSprite.Position;
            }
        }

        private Sprite lHandSprite;
        private Sprite rHandSprite;
        private Sprite lHandActiveSprite;
        private Sprite rHandActiveSprite;
        private Sprite lHandObjectSprite;
        private Sprite rHandObjectSprite;

        private bool lHandActive = true;
        private bool rHandActive = false;

        public HumanHandsGui(PlayerController _playerController)
            : base(_playerController)
        {
            lHandSprite = ResMgr.Singleton.GetSprite("l_hand");
            rHandSprite = ResMgr.Singleton.GetSprite("r_hand");
            lHandActiveSprite = ResMgr.Singleton.GetSprite("l_hand_highlight");
            rHandActiveSprite = ResMgr.Singleton.GetSprite("r_hand_highlight");


        }

        public void ActivateLeftHand()
        {
            lHandActive = true;
            rHandActive = false;
            playerController.SendVerb("selectlefthand",playerController.controlledAtom.uid);

        }

        public void ActivateRightHand()
        {
            lHandActive = false;
            rHandActive = true;
            playerController.SendVerb("selectrighthand", playerController.controlledAtom.uid);
        }

        public void UpdateRightHandObject()
        {
            if (playerController.controlledAtom == null)
                return;

            var mob = (Atom.Mob.Mob)playerController.controlledAtom;
            if (mob.appendages["RightHand"].attachedItem != null)
                rHandObjectSprite = ResMgr.Singleton.GetSprite(mob.appendages["RightHand"].attachedItem.spritename);
            else
                rHandObjectSprite = null;
        }

        private void UpdateLeftHandObject()
        {
            if (playerController.controlledAtom == null)
                return;

            var mob = (Atom.Mob.Mob)playerController.controlledAtom;
            if (mob.appendages["LeftHand"].attachedItem != null)
                lHandObjectSprite = ResMgr.Singleton.GetSprite(mob.appendages["LeftHand"].attachedItem.spritename);
            else
                lHandObjectSprite = null;
        }

        public override void Render()
        {
            if (lHandActive)
                lHandActiveSprite.Draw();
            else
                lHandSprite.Draw();

            if (rHandActive)
                rHandActiveSprite.Draw();
            else
                rHandSprite.Draw();

            if (lHandObjectSprite != null)
            {
                lHandObjectSprite.SetPosition(lHandSprite.Position.X + 45, lHandSprite.Position.Y + 25);
                lHandObjectSprite.Color = System.Drawing.Color.White;
                lHandObjectSprite.Rotation = 0f;
                lHandObjectSprite.Draw();
            }
            if (rHandObjectSprite != null)
            {
                rHandObjectSprite.SetPosition(rHandSprite.Position.X + 45, rHandSprite.Position.Y + 25);
                rHandObjectSprite.Color = System.Drawing.Color.White;
                rHandObjectSprite.Rotation = 0f;
                rHandObjectSprite.Draw();
            }
        }

        public override void HandleNetworkMessage(NetIncomingMessage message)
        {
            HandsComponentMessage messageType = (HandsComponentMessage)message.ReadByte();
            switch (messageType)
            {
                case HandsComponentMessage.SelectHand:
                    HandleSelectHand(message);
                    break;
                case HandsComponentMessage.UpdateHandObjects:
                    HandleUpdateHandObjects();
                    break;
                default: break;
            }
        }

        private void HandleUpdateHandObjects()
        {
            UpdateLeftHandObject();
            UpdateRightHandObject();
        }

        private void HandleSelectHand(NetIncomingMessage message)
        {
            throw new NotImplementedException();
        }
    }
}
