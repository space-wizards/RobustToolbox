using SS14.Shared.IoC;
using SS14.Shared.Log;

namespace SS14.Shared.Interfaces.Log
{
    /// <summary>
    /// Handles logging of messages on specific warning levels.
    /// Output method is dependent on implementation.
    /// </summary>
    public interface ILogManager
    {
        ISawmill RootSawmill { get; }

        /// <summary>
        ///     Gets the sawmill with the specified name. Creates a new one if necessary.
        /// </summary>
        ISawmill GetSawmill(string name);
    }
}
