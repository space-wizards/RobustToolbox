using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.ViewVariables;

namespace Robust.Client.Player
{
    /// <summary>
    ///     Variables and functions that deal with the local client's session.
    /// </summary>
    public sealed class LocalPlayer
    {
        public LocalPlayer(ICommonSession session)
        {
            Session = session;
        }

        /// <summary>
        ///     Game entity that the local player is controlling. If this is default, the player is not attached to any
        ///     entity at all.
        /// </summary>
        [ViewVariables]
        public EntityUid? ControlledEntity => Session.AttachedEntity;

        [ViewVariables]
        public NetUserId UserId => Session.UserId;

        /// <summary>
        ///     OOC name of the local player.
        /// </summary>
        [ViewVariables]
        public string Name => Session.Name;

        /// <summary>
        ///     Session of the local client.
        /// </summary>
        public  ICommonSession Session;
    }
}
