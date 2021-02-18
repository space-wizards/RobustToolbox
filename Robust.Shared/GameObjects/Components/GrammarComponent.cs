using System;
using Robust.Shared.Localization.Macros;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    /// Holds the necessary information to generate text related to the entity.
    /// </summary>
    [RegisterComponent]
    public class GrammarComponent: Component, IProperNamable
    {
        public sealed override string Name => "Grammar";

        public sealed override uint? NetID => NetIDs.GRAMMAR;

        private bool _proper;

        [DataField("proper")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Proper
        {
            get => _proper;
            set
            {
                _proper = value;
                Dirty();
            }
        }

        public override ComponentState GetComponentState()
        {
            return new GrammarComponentState(Proper);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (!(curState is GrammarComponentState cast))
                return;

            Proper = cast.Proper;
        }

        [Serializable, NetSerializable]
        protected sealed class GrammarComponentState : ComponentState
        {
            public GrammarComponentState(bool proper) : base(NetIDs.GRAMMAR)
            {
                Proper = proper;
            }

            public bool Proper { get; }
        }
    }
}
