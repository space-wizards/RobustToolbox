using DiscordRPC;
using DiscordRPC.Logging;
using Robust.Client.Interfaces.Utility;
using Robust.Shared.Log;
using LogLevel = DiscordRPC.Logging.LogLevel;

namespace Robust.Client.Utility
{
    class DiscordRichPresence : IDiscordRichPresence
    {
        private DiscordRpcClient _client;

        private RichPresence _presence = new RichPresence()
        {
            Details = "devstation",
            State = "Testing Rich Presence",
            Assets = new Assets()
            {
                LargeImageKey = "devstation",
                LargeImageText = "I think coolsville SUCKS",
                SmallImageKey = "logo"
            }
        };

        public void Connect()
        {
            // Create the client
            _client = new DiscordRpcClient("560499552273170473")
            {
                Logger = new NativeLogger()
            };
            // == Subscribe to some events
            _client.OnReady += (sender, msg) =>
            {
                _client.Logger.Info("Connected to discord with user {0}", msg.User.Username);
            };

            _client.OnPresenceUpdate += (sender, msg) =>
            {
                _client.Logger.Info("Presence has been updated! ");
            };

            // == Initialize
            _client.Initialize();

            // == Set the presence
            _client.SetPresence(_presence);
        }

        public void Update(string serverName, string Username, string maxUser)
        {
            //TODO: tests
            _presence.Details = "On server: " + serverName;
            _presence.State = "Max players: " + maxUser;
            _presence.Assets.LargeImageText = "Character: " + Username;
            _client.SetPresence(_presence);
        }

        public void Restore()
        {
            _presence = default;
            _client.SetPresence(_presence);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private class NativeLogger : ILogger
        {
            public void Trace(string message, params object[] args)
            {
                if (Level > LogLevel.Trace)
                {
                    return;
                }
                Logger.DebugS("discord", message, args);
            }

            public void Info(string message, params object[] args)
            {
                if (Level > LogLevel.Info)
                {
                    return;
                }
                Logger.InfoS("discord", message, args);
            }

            public void Warning(string message, params object[] args)
            {
                if (Level > LogLevel.Warning)
                {
                    return;
                }
                Logger.WarningS("discord", message, args);
            }

            public void Error(string message, params object[] args)
            {
                Logger.ErrorS("discord", message, args);
            }

            public LogLevel Level { get; set; } = LogLevel.Info;
        }
    }
}
