using SS14.Client.Graphics.Shaders;
using SS14.Client.Interfaces.GameObjects;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Shared.GameObjects;
using SS14.Shared.Input;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Map;

namespace SS14.Client.GameObjects
{
    // Notice: Most actual logic for clicking is done by the game screen.
    public class ClickableComponent : Component, IClientClickableComponent
    {
        private string _baseShader;
        private string _selectionShader;

        public override string Name => "Clickable";
        public override uint? NetID => NetIDs.CLICKABLE;

        public string BaseShader { get => _baseShader; private set => _baseShader = value; }
        public string SelectionShader { get => _selectionShader; set => _selectionShader = value; }

        public override void ExposeData(Shared.Serialization.ObjectSerializer serializer)
        {
            serializer.DataFieldCached(ref _baseShader, "baseshader", "shaded");
            serializer.DataFieldCached(ref _selectionShader, "selectionshader", "selection_outline");
        }

        public bool CheckClick(GridLocalCoordinates worldPos, out int drawdepth)
        {
            var component = Owner.GetComponent<IClickTargetComponent>();

            drawdepth = (int)component.DrawDepth;
            return true;
        }

        public void DispatchClick(IEntity user, ClickType clickType)
        {
            var message = new ClientEntityClickMsg(user.Uid, clickType);
            SendMessage(message);
            if (!IsClientSide)
            {
                SendNetworkMessage(message);
            }
        }

        public void OnMouseEnter()
        {
            var sprite = Owner.GetComponent<ISpriteComponent>();
            sprite.LayerSetShader(0, SelectionShader);
        }

        public void OnMouseLeave()
        {
            var sprite = Owner.GetComponent<ISpriteComponent>();
            sprite.LayerSetShader(0, BaseShader);
        }
    }
}
