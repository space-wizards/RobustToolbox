using System;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects.Components.UserInterface
{
    public abstract class SharedUserInterfaceComponent : Component
    {
        public sealed override string Name => "UserInterface";
        public sealed override uint? NetID => NetIDs.USERINTERFACE;

        public sealed class PrototypeData : IExposeData
        {
            public object UiKey { get; private set; } = default!;
            public string ClientType { get; private set; } = default!;

            public void ExposeData(ObjectSerializer serializer)
            {
                UiKey = serializer.ReadStringEnumKey("key");
                ClientType = serializer.ReadDataField<string>("type");
            }
        }

        [NetSerializable, Serializable]
        protected sealed class BoundInterfaceMessageWrapMessage : ComponentMessage
        {
            public readonly BoundUserInterfaceMessage Message;
            public readonly object UiKey;

            public BoundInterfaceMessageWrapMessage(BoundUserInterfaceMessage message, object uiKey)
            {
                Directed = true;
                Message = message;
                UiKey = uiKey;
            }

            public override string ToString()
            {
                return $"{nameof(BoundInterfaceMessageWrapMessage)}: {Message}";
            }
        }
    }

    [NetSerializable, Serializable]
    public abstract class BoundUserInterfaceState
    {
    }


    [NetSerializable, Serializable]
    public class BoundUserInterfaceMessage
    {
    }

    [NetSerializable, Serializable]
    internal sealed class UpdateBoundStateMessage : BoundUserInterfaceMessage
    {
        public readonly BoundUserInterfaceState State;

        public UpdateBoundStateMessage(BoundUserInterfaceState state)
        {
            State = state;
        }
    }

    [NetSerializable, Serializable]
    internal sealed class OpenBoundInterfaceMessage : BoundUserInterfaceMessage
    {
    }

    [NetSerializable, Serializable]
    internal sealed class CloseBoundInterfaceMessage : BoundUserInterfaceMessage
    {
    }
}
