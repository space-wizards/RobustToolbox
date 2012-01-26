using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using GorgonLibrary.GUI;
using SS3D.UserInterface;
using Lidgren.Network;
using SS3D_shared;
using ClientResourceManager;
using CGO;
using SS3D_shared.GO;
using SS3D.Modules;
using SS3D.Modules.Network;

namespace SS3D.UserInterface
{
    class TargetingDummy : GuiComponent
    {
        List<TargetingDummyElement> elements = new List<TargetingDummyElement>();
        private NetworkManager netMgr;

        public TargetingDummy(PlayerController controller, NetworkManager _netMgr)
            : base(controller)
        {
            netMgr = _netMgr;
            TargetingDummyElement head = new TargetingDummyElement("dummy_head", BodyPart.head, controller);
            TargetingDummyElement torso = new TargetingDummyElement("dummy_torso", BodyPart.torso, controller);
            TargetingDummyElement groin = new TargetingDummyElement("dummy_groin", BodyPart.groin, controller);
            TargetingDummyElement arm_l = new TargetingDummyElement("dummy_arm_l", BodyPart.arm_l, controller);
            TargetingDummyElement arm_r = new TargetingDummyElement("dummy_arm_r", BodyPart.arm_r, controller);
            TargetingDummyElement leg_l = new TargetingDummyElement("dummy_leg_l", BodyPart.leg_l, controller);
            TargetingDummyElement leg_r = new TargetingDummyElement("dummy_leg_r", BodyPart.leg_r, controller);
            elements.Add(head);
            elements.Add(torso);
            elements.Add(groin);
            elements.Add(arm_l);
            elements.Add(arm_r);
            elements.Add(leg_l);
            elements.Add(leg_r);
            head.Clicked += new TargetingDummyElement.TargetingDummyElementPressHandler(Selected);
            torso.Clicked += new TargetingDummyElement.TargetingDummyElementPressHandler(Selected);
            groin.Clicked += new TargetingDummyElement.TargetingDummyElementPressHandler(Selected);
            arm_l.Clicked += new TargetingDummyElement.TargetingDummyElementPressHandler(Selected);
            arm_r.Clicked += new TargetingDummyElement.TargetingDummyElementPressHandler(Selected);
            leg_l.Clicked += new TargetingDummyElement.TargetingDummyElementPressHandler(Selected);
            leg_r.Clicked += new TargetingDummyElement.TargetingDummyElementPressHandler(Selected);
            Update();
        }

        void Selected(TargetingDummyElement sender)
        {
            //Send server targeted location
            NetOutgoingMessage msg = netMgr.netClient.CreateMessage();
            msg.Write((byte)NetMessage.PlayerSessionMessage);
            msg.Write((byte)PlayerSessionMessage.SetTargetArea);
            msg.Write((byte)sender.myPart);
            netMgr.SendMessage(msg, NetDeliveryMethod.ReliableOrdered);
        }

        public override void Update()
        {
            Entity entity;

            if (playerController != null)
                entity = (Entity)playerController.controlledAtom;
            else
                entity = null;

            clientArea = new Rectangle(Position, new Size((int)elements[0].ClientArea.Width, (int)elements[0].ClientArea.Height));
            foreach (TargetingDummyElement current in elements)
            {
                if (entity != null && entity.HasComponent(ComponentFamily.Damageable))
                {
                    List<ComponentReplyMessage> replies = new List<ComponentReplyMessage>();
                    entity.SendMessage(this, ComponentMessageType.GetCurrentLocationHealth, replies, current.myPart);
                    if (replies.Any(x => x.messageType == ComponentMessageType.CurrentLocationHealth))
                    {
                        ComponentReplyMessage reply = replies.First(x => x.messageType == ComponentMessageType.CurrentLocationHealth);
                        current.currHealth = (int)reply.paramsList[1];
                        current.maxHealth = (int)reply.paramsList[2];
                    }
                }
                current.Position = this.Position;
                current.Update();
            }
        }

        public override void Render()
        {
            foreach (TargetingDummyElement current in elements)
                current.Render();
        }

        public override void Dispose()
        {
            foreach (TargetingDummyElement current in elements)
                current.Dispose();

            base.Dispose();
            GC.SuppressFinalize(this);
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            if (!clientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y))) return false;

            TargetingDummyElement prevSelection = (from element in elements
                                                   where element.isSelected()
                                                   select element).FirstOrDefault();

            foreach (TargetingDummyElement toClear in elements) toClear.ClearSelected();

            foreach (TargetingDummyElement current in elements.ToArray()) //To array because list order changes in loop.
            {
                if (current.MouseDown(e))
                {
                    elements = (from a in elements
                                orderby (a == current) ascending
                                select a).ToList();
                    return true;
                }
            }

            if (prevSelection != null) prevSelection.Select();

            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            return false;
        }
    }
}
