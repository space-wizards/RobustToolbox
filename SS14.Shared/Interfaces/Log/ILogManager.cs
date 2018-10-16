using System.Diagnostics.Contracts;

namespace SS14.Shared.Interfaces.Log
{
    /// <summary>
    ///     Manages logging sawmills.
    /// </summary>
    /// <seealso cref="ISawmill"/>
    public interface ILogManager
    {
        /// <summary>
        ///     The "root" sawmill every other sawmill is parented to.
        /// </summary>
        ISawmill RootSawmill { get; }

        /// <summary>
        ///     Gets the sawmill with the specified name. Creates a new one if necessary.
        /// </summary>
        [Pure]
        ISawmill GetSawmill(string name);
    }
}
