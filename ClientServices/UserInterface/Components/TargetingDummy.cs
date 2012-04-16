using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using ClientInterfaces;
using ClientInterfaces.Network;
using ClientInterfaces.Player;
using ClientInterfaces.Resource;
using GorgonLibrary.InputDevices;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;

namespace ClientServices.UserInterface.Components
{
    class TargetingDummy : GuiComponent
    {
        private readonly IPlayerManager _playerManager;
        private readonly INetworkManager _networkManager;
        private readonly IResourceManager _resourceManager;

        private List<TargetingDummyElement> _elements = new List<TargetingDummyElement>();
        
        public TargetingDummy(IPlayerManager playerManager, INetworkManager networkManager, IResourceManager resourceManager)
        {
            _networkManager = networkManager;
            _playerManager = playerManager;
            _resourceManager = resourceManager;

            var head = new TargetingDummyElement("dummy_head", BodyPart.Head, _resourceManager);
            var torso = new TargetingDummyElement("dummy_torso", BodyPart.Torso, _resourceManager);
            var groin = new TargetingDummyElement("dummy_groin", BodyPart.Groin, _resourceManager);
            var armL = new TargetingDummyElement("dummy_arm_l", BodyPart.Left_Arm, _resourceManager);
            var armR = new TargetingDummyElement("dummy_arm_r", BodyPart.Right_Arm, _resourceManager);
            var legL = new TargetingDummyElement("dummy_leg_l", BodyPart.Left_Leg, _resourceManager);
            var legR = new TargetingDummyElement("dummy_leg_r", BodyPart.Right_Leg, _resourceManager);

            _elements.Add(head);
            _elements.Add(torso);
            _elements.Add(groin);
            _elements.Add(armL);
            _elements.Add(armR);
            _elements.Add(legL);
            _elements.Add(legR);
            head.Clicked += Selected;
            torso.Clicked += Selected;
            groin.Clicked += Selected;
            armL.Clicked += Selected;
            armR.Clicked += Selected;
            legL.Clicked += Selected;
            legR.Clicked += Selected;
            Update(0);
            UpdateHealthIcon();
        }

        void Selected(TargetingDummyElement sender)
        {
            //Send server targeted location
            var msg = _networkManager.CreateMessage();
            msg.Write((byte)NetMessage.PlayerSessionMessage);
            msg.Write((byte)PlayerSessionMessage.SetTargetArea);
            msg.Write((byte)sender.BodyPart);
            _networkManager.SendMessage(msg, NetDeliveryMethod.ReliableOrdered);
        }

        public void UpdateHealthIcon()
        {
            var entity = _playerManager.ControlledEntity;

            if (entity == null) return;

            foreach (var current in _elements)
            {
                if (entity != null && entity.HasComponent(ComponentFamily.Damageable))
                {
                    var reply = entity.SendMessage(this, ComponentFamily.Damageable, ComponentMessageType.GetCurrentLocationHealth, current.BodyPart);
                    if (reply.MessageType == ComponentMessageType.CurrentLocationHealth)
                    {
                        current.CurrentHealth = (int)reply.ParamsList[1];
                        current.MaxHealth = (int)reply.ParamsList[2];
                    }
                }
            }
        }

        public override sealed void Update(float frameTime)
        {
            ClientArea = new Rectangle(Position, new Size(_elements[0].ClientArea.Width, _elements[0].ClientArea.Height));
            foreach (var current in _elements)
            {
                current.Position = Position;
                current.Update(frameTime);
            }
        }

        public override void Render()
        {
            foreach (var current in _elements)
                current.Render();
        }

        public override void Dispose()
        {
            foreach (var current in _elements)
                current.Dispose();

            base.Dispose();
            GC.SuppressFinalize(this);
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            if (!ClientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y))) return false;

            TargetingDummyElement prevSelection = (from element in _elements
                                                   where element.IsSelected()
                                                   select element).FirstOrDefault();

            foreach (TargetingDummyElement toClear in _elements) toClear.ClearSelected();

            foreach (TargetingDummyElement current in _elements.ToArray()) //To array because list order changes in loop.
            {
                if (current.MouseDown(e))
                {
                    _elements = (from a in _elements
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
