using SS14.Shared.Interfaces.Log;
using SS14.Shared.IoC;

namespace SS14.Server.Interfaces.Log
{
    public interface IServerLogManager : ILogManager, IIoCInterface
    {
        /// <summary>
        /// The file path to log to.
        /// If this is changed, the logger should close the current file (if any) and open the new one.
        /// </summary>
        string LogPath { get; set; }
    }
}
