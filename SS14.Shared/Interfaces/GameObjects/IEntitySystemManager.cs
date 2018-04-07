using SS14.Shared.IoC;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.System;
using SS14.Shared.Network.Messages;

namespace SS14.Shared.Interfaces.GameObjects
{
    /// <summary>
    /// Like SS13's master controller. It controls <see cref="IEntitySystem"/> instances.
    /// These instances have set of rules for which components they can run over.
    /// </summary>
    /// <remarks>
    /// The management of these amounts to a couple of things:
    /// <list type="bullet">
    /// <item>
    /// <description>Periodically ticking them through <see cref="IEntitySystem.Update(float)"/>.</description>
    /// </item>
    /// <item>
    /// <description>
    /// Relaying <see cref="EntitySystemData"/> messages from the network through
    /// <see cref="IEntitySystem.HandleNetMessage"/>.
    /// </description>
    /// </item>
    /// </list>
    /// Periodically ticks <see cref="IEntitySystem"/> instances.
    /// </remarks>
    /// <seealso cref="IEntitySystem"/>
    public interface IEntitySystemManager
    {
        /// <summary>
        /// Register an <see cref="EntitySystemMessage"/> type to be sent to the specified system.
        /// </summary>
        /// <typeparam name="T">The type of system message that will be relayed.</typeparam>
        /// <param name="regSystem">The <see cref="IEntitySystem"/> that will be receiving the messages.</param>
        /// <seealso cref="HandleSystemMessage(EntitySystemData)"/>
        void RegisterMessageType<T>(IEntitySystem regSystem) where T : EntitySystemMessage;

        /// <summary>
        /// Get an entity system of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of entity system to find.</typeparam>
        /// <returns>The <see cref="IEntitySystem"/> instance matching the specified type.</returns>
        T GetEntitySystem<T>() where T : IEntitySystem;

        /// <summary>
        /// Initialize, discover systems and initialize them through <see cref="IEntitySystem.Initialize"/>.
        /// </summary>
        /// <seealso cref="IEntitySystem.Initialize"/>
        void Initialize();

        /// <summary>
        /// Clean up, shut down all systems through <see cref="IEntitySystem.Shutdown"/> and remove them.
        /// </summary>
        /// <seealso cref="IEntitySystem.Shutdown"/>
        void Shutdown();

        /// <summary>
        /// Handle an <see cref="EntitySystemData"/> by routing it to the registered <see cref="IEntitySystem"/>.
        /// </summary>
        /// <param name="sysMsg">The message to route.</param>
        /// <seealso cref="RegisterMessageType{T}(IEntitySystem)"/>
        /// <seealso cref="IEntitySystem.HandleNetMessage"/>
        void HandleSystemMessage(MsgEntity sysMsg);

        /// <summary>
        /// Update all systems.
        /// </summary>
        /// <param name="frameTime">Time since the last frame was rendered.</param>
        /// <seealso cref="IEntitySystem.Update(float)"/>
        void Update(float frameTime);
        void FrameUpdate(float frameTime);
    }
}
