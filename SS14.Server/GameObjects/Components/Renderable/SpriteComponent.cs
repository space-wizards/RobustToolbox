using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using System.Collections.Generic;
using System;
using SS14.Shared.GameObjects.Serialization;
using SS14.Shared.Maths;

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

        /// <summary>
        ///     Offsets the sprite from the entity origin by this many meters.
        /// </summary>
        private Vector2 _offset;
        private bool _visible;
        private DrawDepth _drawDepth;

        public SpriteComponent()
        {
            _slaves = new List<IRenderableComponent>();
        }

        public DrawDepth DrawDepth
        {
            get => _drawDepth;
            set => _drawDepth = value;
        }

        public bool Visible
        {
            get => _visible;
            set => _visible = value;
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
        
        public override void ExposeData(EntitySerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _visible, "visible", true);
            serializer.DataField(ref _drawDepth, "depth", DrawDepth.FloorTiles);
            serializer.DataField(ref _currentSpriteKey, "skey", String.Empty);
            serializer.DataField(ref _currentBaseName, "sbase", String.Empty);
            serializer.DataField(ref _offset, "offset", Vector2.Zero);
        }

        public override ComponentState GetComponentState()
        {
            return new SpriteComponentState(Visible, DrawDepth, _currentSpriteKey, _currentBaseName, _offset);
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
