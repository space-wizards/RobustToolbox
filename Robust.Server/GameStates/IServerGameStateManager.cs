namespace Robust.Server.GameStates
{
    /// <summary>
    /// Engine service that provides creating and dispatching of game states.
    /// </summary>
    public interface IServerGameStateManager
    {
        /// <summary>
        /// One time initialization of the service.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Create and dispatch game states to all connected sessions.
        /// </summary>
        void SendGameStateUpdate();

        bool PvsEnabled { get; set; }
        float PvsRange { get; set; }
    }
}
