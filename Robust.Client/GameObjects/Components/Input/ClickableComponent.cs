using System;
using Robust.Client.Graphics.Shaders;
using Robust.Client.Interfaces.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
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
        private Box2? _localBounds;

        public override string Name => "Clickable";
        public override uint? NetID => NetIDs.CLICKABLE;
        public override Type StateType => typeof(ClickableComponentState);

        [ViewVariables]
        public Box2? LocalBounds
        {
            get => _localBounds;
            set => _localBounds = value;
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public string SelectionShader => _selectionShader;

        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataFieldCached(ref _selectionShader, "selectionshader", "selection_outline");
            serializer.DataField(ref _localBounds, "bounds", null);
        }

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            _selectionShaderInstance = _prototypeManager.Index<ShaderPrototype>(_selectionShader).Instance();
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            var state = (ClickableComponentState)curState;
            _localBounds = state.LocalBounds;
        }

        /// <inheritdoc />
        //TODO: This needs to accept MapPosition, not a Vector2
        public bool CheckClick(Vector2 worldPos, out int drawdepth)
        {
            if (LocalBounds.HasValue)
            {
                var worldBounds = LocalBounds.Value.Translated(Owner.Transform.WorldPosition);
                if (!worldBounds.Contains(worldPos))
                {
                    drawdepth = default;
                    return false;
                }
            }

            if (Owner.TryGetComponent(out ISpriteComponent sprite) && !sprite.Visible)
            {
                drawdepth = default;
                return false;
            }

            var component = Owner.GetComponent<IClickTargetComponent>();
            drawdepth = (int)component.DrawDepth;
            return true;
        }

        /// <inheritdoc />
        public void DispatchClick(IEntity user, ClickType clickType)
        {
            var message = new ClientEntityClickMsg(user.Uid, clickType);
            SendMessage(message);
        }

        /// <inheritdoc />
        public void OnMouseEnter()
        {
            if (Owner.TryGetComponent(out ISpriteComponent sprite))
            {
                sprite.PostShader = _selectionShaderInstance;
                sprite.RenderOrder = Owner.EntityManager.CurrentTick.Value;
            }
        }

        /// <inheritdoc />
        public void OnMouseLeave()
        {
            if (Owner.TryGetComponent(out ISpriteComponent sprite))
            {
                sprite.PostShader = null;
                sprite.RenderOrder = 0;
            }
        }
    }
}
