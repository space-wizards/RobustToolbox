using System;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Player;

/// <summary>
/// Helper functions for subscribing to <see cref="ISharedPlayerManager"/> events from entity systems.
/// </summary>
/// <remarks>
/// Functions here automatically unsubscribe the entity system when shut down.
/// </remarks>
public static class PlayerManagerSubscriptionExt
{
    /// <summary>
    /// Subscribe to <see cref="ISharedPlayerManager.PlayerStatusChanged"/>.
    /// </summary>
    /// <param name="subs">
    /// The subscriptions object of the entity system you're subscribing from.
    /// </param>
    /// <param name="playerManager">The player manager to subscribe on.</param>
    /// <param name="handler">The callback to be run when a player's status changed.</param>
    public static void PlayerStatusChanged(
        this EntitySystem.Subscriptions subs,
        ISharedPlayerManager playerManager,
        EventHandler<SessionStatusEventArgs> handler)
    {
        subs.RegisterUnsubscription(() =>
        {
            playerManager.PlayerStatusChanged -= handler;
        });

        playerManager.PlayerStatusChanged += handler;
    }
}
