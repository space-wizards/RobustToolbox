using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13_Shared;

namespace ServerInterfaces.Configuration
{
    public interface IConfigurationManager
    {
        void Initialize(string configFilePath);
        bool MessageLogging { get; set; }
        string ServerName { get; set; }
        string ServerMapName { get; set; }
        string ServerWelcomeMessage { get; set; }
        string AdminPassword { get; set; }
        string LogPath { get; set; }
        int Version { get; }
        int Port { get; set; }
        int ServerMaxPlayers { get; set; }
        int FramePeriod { get; set; }
        GameType GameType { get; set; }
    }
}
