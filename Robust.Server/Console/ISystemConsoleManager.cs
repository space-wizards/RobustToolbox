namespace Robust.Server.Console
{
    /// <summary>
    ///     Wraps the system console.
    /// </summary>
    public interface ISystemConsoleManager
    {
        /// <summary>
        ///     process input/output of the console. This needs to be called often.
        /// </summary>
        void Update();

        /// <summary>
        ///     Prints <paramref name="text" /> to the system console.
        /// </summary>
        /// <param name="text">Text to write to the system console.</param>
        void Print(string text);
    }
}
