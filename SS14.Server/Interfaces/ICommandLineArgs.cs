using SS14.Shared.IoC;

namespace SS14.Server.Interfaces
{
    public interface ICommandLineArgs : IIoCInterface
    {
        string ConfigFile { get; }
    }
}
