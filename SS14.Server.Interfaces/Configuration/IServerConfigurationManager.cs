using SFML.System;
using SS14.Shared;
using SS14.Shared.ServerEnums;
using SS14.Shared.IoC;

namespace SS14.Server.Interfaces.Configuration
{
    public interface IServerConfigurationManager : IIoCInterface
    {
        bool MessageLogging { get; set; }
        string ServerName { get; set; }
        string ServerMapName { get; set; }
        string ServerWelcomeMessage { get; set; }
        string LogPath { get; set; }
        LogLevel LogLevel { get; set; }
        int Version { get; }
        int Port { get; set; }
        int ServerMaxPlayers { get; set; }
        float TickRate { get; set; }
        GameType GameType { get; set; }
        Vector2i ConsoleSize { get; set; }
        void Initialize(string configFilePath);
        void Save();
    }
}
