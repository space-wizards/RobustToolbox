using SS14.Shared.IoC;

namespace SS14.Client.Interfaces
{
    interface IGameController : IIoCInterface
    {
        /// <summary>
        /// Main method that does everything, starting the game loop.
        /// Exits when the client shuts down.
        /// </summary>
        void Run();
    }
}
