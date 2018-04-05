using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Players;

namespace SS14.Shared.Interfaces.GameObjects.System
{
    /// <summary>
    ///     A subsystem that acts on all components of a type at once.
    ///     Entity systems are similar to TGstation13 subsystems.
    ///     They have a set of entities to run over and run every once in a while.
    ///     They get managed by an <see cref="IEntitySystemManager" />.
    /// </summary>
    public interface IEntitySystem
    {
        void RegisterMessageTypes();

        void SubscribeEvents();

        /// <summary>
        ///     Called once when the system is created to initialize its state.
        /// </summary>
        void Initialize();

        /// <summary>
        ///     Called once before the system is destroyed so that the system can clean up.
        /// </summary>
        void Shutdown();

        /// <summary>
        /// Handler for all incoming network messages.
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="message"></param>
        void HandleNetMessage(INetChannel channel, EntitySystemMessage message);

        /// <summary>
        ///     Called once per frame/tick to update the system.
        /// </summary>
        /// <param name="frameTime">Delta time since Update() was last called.</param>
        void Update(float frameTime);
        void FrameUpdate(float frameTime);
    }
}
