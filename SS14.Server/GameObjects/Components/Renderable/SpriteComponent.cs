using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using System.Collections.Generic;

namespace SS14.Server.GameObjects
{
    public class SpriteComponent : Component, ISpriteRenderableComponent
    {
        public override string Name => "Sprite";
        public override uint? NetID => NetIDs.SPRITE;
        private IRenderableComponent _master;
        private readonly List<IRenderableComponent> _slaves;
        private string _currentBaseName;
        private string _currentSpriteKey;

        public SpriteComponent()
        {
            _slaves = new List<IRenderableComponent>();
        }

        public DrawDepth DrawDepth { get; set; } = DrawDepth.FloorTiles;

        public bool Visible { get; set; } = true;

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
            return new SpriteComponentState(Visible, DrawDepth, _currentSpriteKey, _currentBaseName);
        }

        public bool IsSlaved()
        {
            return _master != null;
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
            _master = mastercompo;
        }

        public void UnsetMaster()
        {
            if (_master == null)
                return;
            _master.RemoveSlave(this);
            _master = null;
        }

        public void AddSlave(IRenderableComponent slavecompo)
        {
            _slaves.Add(slavecompo);
        }

        public void RemoveSlave(IRenderableComponent slavecompo)
        {
            if (_slaves.Contains(slavecompo))
                _slaves.Remove(slavecompo);
        }
    }
}
