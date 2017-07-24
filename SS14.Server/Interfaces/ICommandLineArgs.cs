using SS14.Shared.IoC;

namespace SS14.Server.Interfaces
{
    public interface ICommandLineArgs
    {
        /// <summary>
        /// Parses command line arguments from the environment.
        /// This method must be ran before other members are accessed.
        /// </summary>
        /// <returns>True if the arguments were parsed correctly, false if the program should terminate immediately (parse error, help used).</returns>
        bool Parse();
        string ConfigFile { get; }
    }
}
