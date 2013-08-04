using System.Drawing;
using SS13_Shared;
using SS13_Shared.ServerEnums;

namespace ServerInterfaces.Configuration
{
    public interface IConfigurationManager
    {
        bool MessageLogging { get; set; }
        string ServerName { get; set; }
        string ServerMapName { get; set; }
        string ServerWelcomeMessage { get; set; }
        string AdminPassword { get; set; }
        string LogPath { get; set; }
        LogLevel LogLevel { get; set; }
        int Version { get; }
        int Port { get; set; }
        int ServerMaxPlayers { get; set; }
        float TickRate { get; set; }
        GameType GameType { get; set; }
        Size ConsoleSize { get; set; }
        void Initialize(string configFilePath);
        void Save();
    }
}