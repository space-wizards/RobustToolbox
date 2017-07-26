using Lidgren.Network;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components;
using SS14.Shared.GameObjects.Components.Renderable;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using System.Collections.Generic;

namespace SS14.Server.GameObjects
{
    public class SpriteComponent : Component, ISpriteRenderableComponent
    {
        public override string Name => "Sprite";
        public override uint? NetID => NetIDs.SPRITE;
        protected IRenderableComponent master;
        protected List<IRenderableComponent> slaves;
        private string _currentBaseName;
        private string _currentSpriteKey;
        private DrawDepth _drawDepth = DrawDepth.FloorTiles;
        private bool visible = true;

        public SpriteComponent()
        {
            slaves = new List<IRenderableComponent>();
        }

        public DrawDepth drawDepth
        {
            get { return _drawDepth; }

            set
            {
                if (value != _drawDepth)
                {
                    _drawDepth = value;
                    SendDrawDepth(null);
                }
            }
        }

        public bool Visible
        {
            get { return visible; }

            set
            {
                if (value == visible) return;
                visible = value;
                SendVisible(null);
            }
        }

        private void SendVisible(NetConnection connection)
        {
        }

        private void SendDrawDepth(NetConnection connection)
        {
        }

        public override ComponentReplyMessage ReceiveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = base.ReceiveMessage(sender, type, list);

            if (sender == this)
                return ComponentReplyMessage.Empty;

            switch (type)
            {
                case ComponentMessageType.SetSpriteByKey:
                    if (Owner != null)
                        _currentSpriteKey = (string)list[0];
                    break;
                case ComponentMessageType.SetBaseName:
                    if (Owner != null)
                        _currentBaseName = (string)list[0];
                    break;
                case ComponentMessageType.SetVisible:
                    Visible = (bool)list[0];
                    break;
            }

            return reply;
        }

        public override ComponentState GetComponentState()
        {
            return new SpriteComponentState(Visible, drawDepth, _currentSpriteKey, _currentBaseName);
        }

        public bool IsSlaved()
        {
            return master != null;
        }

        public void SetMaster(IEntity m)
        {
            if (!m.HasComponent<ISpriteRenderableComponent>())
                return;
            var mastercompo = m.GetComponent<ISpriteRenderableComponent>();
            //If there's no sprite component, then FUCK IT
            if (mastercompo == null)
                return;

            mastercompo.AddSlave(this);
            master = mastercompo;
        }

        public void UnsetMaster()
        {
            if (master == null)
                return;
            master.RemoveSlave(this);
            master = null;
        }

        public void AddSlave(IRenderableComponent slavecompo)
        {
            slaves.Add(slavecompo);
        }

        public void RemoveSlave(IRenderableComponent slavecompo)
        {
            if (slaves.Contains(slavecompo))
                slaves.Remove(slavecompo);
        }
    }
}
