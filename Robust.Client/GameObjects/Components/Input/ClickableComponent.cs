using Robust.Client.Graphics.Shaders;
using Robust.Client.Interfaces.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Client.GameObjects
{
    // Notice: Most actual logic for clicking is done by the game screen.
    public class ClickableComponent : Component, IClientClickableComponent
    {
#pragma warning disable 649
        [Dependency] private readonly IPrototypeManager _prototypeManager;
#pragma warning restore 649

        private ShaderInstance _selectionShaderInstance;

        private string _selectionShader;

        public override string Name => "Clickable";
        public override uint? NetID => NetIDs.CLICKABLE;

        [ViewVariables(VVAccess.ReadWrite)]
        public string SelectionShader => _selectionShader;

        public override void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataFieldCached(ref _selectionShader, "selectionshader", "selection_outline");
        }

        public override void Initialize()
        {
            base.Initialize();

            _selectionShaderInstance = _prototypeManager.Index<ShaderPrototype>(_selectionShader).Instance();
        }

        public bool CheckClick(GridCoordinates worldPos, out int drawdepth)
        {
            var component = Owner.GetComponent<IClickTargetComponent>();

            if (Owner.TryGetComponent(out ISpriteComponent sprite) && !sprite.Visible)
            {
                drawdepth = default;
                return false;
            }

            drawdepth = (int)component.DrawDepth;
            return true;
        }

        public void DispatchClick(IEntity user, ClickType clickType)
        {
            var message = new ClientEntityClickMsg(user.Uid, clickType);
            SendMessage(message);
        }

        public void OnMouseEnter()
        {
            if (Owner.TryGetComponent(out ISpriteComponent sprite))
            {
                sprite.PostShader = _selectionShaderInstance;
            }
        }

        public void OnMouseLeave()
        {
            if (Owner.TryGetComponent(out ISpriteComponent sprite))
            {
                sprite.PostShader = null;
            }
        }
    }
}
