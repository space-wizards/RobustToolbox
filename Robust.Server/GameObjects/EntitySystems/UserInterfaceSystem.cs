using Robust.Shared.GameObjects;

namespace Robust.Server.GameObjects;

public sealed class UserInterfaceSystem : SharedUserInterfaceSystem
{
    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<BoundUIWrapMessage>(OnMessageReceived);
    }
}
