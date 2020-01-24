using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Robust.Server.GameObjects
{
    public class ClickableComponent : Component, IClickableComponent
    {
        private Box2? _localBounds;

        public Box2? LocalBounds
        {
            get => _localBounds;
            set => _localBounds = value;
        }

        public override string Name => "Clickable";
        public override uint? NetID => NetIDs.CLICKABLE;

        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _localBounds, "bounds", null);
        }

        /// <inheritdoc />
        public override ComponentState GetComponentState()
        {
            return new ClickableComponentState(_localBounds);
        }
    }
}
