using GameObject;
using Lidgren.Network;
using SS13_Shared.GO;
using SS13_Shared.GO.Component.Renderable;

namespace SGO
{
    public class SpriteComponent : Component
    {
        private bool visible = true;

        public DrawDepth drawDepth
        {
            get
            {
                return _drawDepth;
            }

            set
            {
                if (value != _drawDepth)
                {
                    _drawDepth = value;
                    SendDrawDepth(null);
                }
            }
        }

        private DrawDepth _drawDepth = DrawDepth.FloorTiles;
        private string _currentSpriteKey;
        private string _currentBaseName;

        public SpriteComponent()
        {
            Family = ComponentFamily.Renderable;
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

        public override void HandleInstantiationMessage(NetConnection netConnection)
        {
            base.HandleInstantiationMessage(netConnection);
            SendVisible(netConnection);
            SendDrawDepth(netConnection);
        }

        private void SendVisible(NetConnection connection)
        {
        }

        private void SendDrawDepth(NetConnection connection)
        {
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = base.RecieveMessage(sender, type, list);

            if (sender == this)
                return ComponentReplyMessage.Empty;

            switch (type)
            {
                case ComponentMessageType.SetSpriteByKey:
                    if (Owner != null)
                        _currentSpriteKey = (string) list[0];
                    break;
                case ComponentMessageType.SetBaseName:
                    if (Owner != null)
                        _currentBaseName = (string) list[0];
                    break;
                case ComponentMessageType.SetVisible:
                    Visible = (bool) list[0];
                    break;
                case ComponentMessageType.SetDrawDepth:
                    if (Owner != null)
                        drawDepth = (DrawDepth)list[0];
                    break;
            }

            return reply;
        }

        public override ComponentState GetComponentState()
        {
            return new SpriteComponentState(Visible, drawDepth, _currentSpriteKey, _currentBaseName);
        }
    }
}