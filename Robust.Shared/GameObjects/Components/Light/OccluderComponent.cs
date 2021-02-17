using System;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects
{
    public class OccluderComponent : Component
    {
        public sealed override string Name => "Occluder";
        public sealed override uint? NetID => NetIDs.OCCLUDER;

        [YamlField("enabled")]
        private bool _enabled = true;
        [YamlField("boundingBox")]
        private Box2 _boundingBox = new(-0.5f, -0.5f, 0.5f, 0.5f);

        [ViewVariables(VVAccess.ReadWrite)]
        public Box2 BoundingBox
        {
            get => _boundingBox;
            set
            {
                _boundingBox = value;
                Dirty();
                BoundingBoxChanged();
            }
        }

        private void BoundingBoxChanged()
        {
            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new OccluderBoundingBoxChangedMessage(this));
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public virtual bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value)
                    return;

                _enabled = value;
                Dirty();
            }
        }

        protected override void Startup()
        {
            base.Startup();

            EntitySystem.Get<OccluderSystem>().AddOrUpdateEntity(Owner, Owner.Transform.Coordinates);
        }

        public override void OnRemove()
        {
            base.OnRemove();

            var transform = Owner.Transform;
            var map = transform.MapID;
            if (map != MapId.Nullspace)
            {
                Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local,
                    new OccluderTreeRemoveOccluderMessage(this, map, transform.GridID));
            }
        }

        public override ComponentState GetComponentState()
        {
            return new OccluderComponentState(Enabled, BoundingBox);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (curState == null)
            {
                return;
            }

            var cast = (OccluderComponentState) curState;

            Enabled = cast.Enabled;
            BoundingBox = cast.BoundingBox;
        }

        [NetSerializable, Serializable]
        private sealed class OccluderComponentState : ComponentState
        {
            public bool Enabled { get; }
            public Box2 BoundingBox { get; }

            public OccluderComponentState(bool enabled, Box2 boundingBox) : base(NetIDs.OCCLUDER)
            {
                Enabled = enabled;
                BoundingBox = boundingBox;
            }
        }
    }

    internal struct OccluderBoundingBoxChangedMessage
    {
        public OccluderComponent Occluder;

        public OccluderBoundingBoxChangedMessage(OccluderComponent occluder)
        {
            Occluder = occluder;
        }
    }
}
