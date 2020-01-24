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
        private Box2? _localBounds;

        public override string Name => "Clickable";
        public override uint? NetID => NetIDs.CLICKABLE;

        [ViewVariables]
        public Box2? LocalBounds
        {
            get => _localBounds;
            set => _localBounds = value;
        }

        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _localBounds, "bounds", null);
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
    }
}
