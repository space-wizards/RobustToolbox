using DiscordRPC;
using DiscordRPC.Logging;
using Robust.Client.Interfaces.Utility;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.IoC;

namespace Robust.Client.Utility
{
    internal sealed class DiscordRichPresence : IDiscordRichPresence, IPostInjectInit
    {
        private static readonly RichPresence _defaultPresence = new RichPresence
        {
            Details = "In Main Menu",
            State = "In Main Menu",
            Assets = new Assets
            {
                LargeImageKey = "devstation",
                LargeImageText = "I think coolsville SUCKS",
                SmallImageKey = "logo"
            }
        };

        private RichPresence _activePresence;

        private DiscordRpcClient _client;

        [Dependency] private readonly IConfigurationManager _configurationManager;
        [Dependency] private readonly ILogManager _logManager;

        private bool _initialized;

        public void Initialize()
        {
            if (_configurationManager.GetCVar<bool>("discord.enabled"))
            {
                _start();
            }

            _initialized = true;
        }

        private void _start()
        {
            _stop();

            // Create the client
            _client = new DiscordRpcClient("560499552273170473")
            {
                Logger = new NativeLogger(_logManager.GetSawmill("discord"))
            };

            // == Subscribe to some events
            _client.OnReady += (sender, msg) =>
            {
                _client.Logger.Info("Connected to discord with user {0}", msg.User.Username);
            };

            _client.OnPresenceUpdate += (sender, msg) => _client.Logger.Info("Presence has been updated! ");

            // == Initialize
            _client.Initialize();

            // == Set the presence
            _client.SetPresence(_activePresence ?? _defaultPresence);
        }

        private void _stop()
        {
            _client?.Dispose();
            _client = null;
        }

        public void Update(string serverName, string username, string maxUser)
        {
            _activePresence = new RichPresence
            {
                Details = $"On Server: {serverName}",
                State = $"Max players: {maxUser}",
                Assets = new Assets
                {
                    LargeImageKey = "devstation",
                    LargeImageText = $"Character: {username}",
                    SmallImageKey = "logo"
                }
            };
            _client?.SetPresence(_activePresence);
        }

        public void ClearPresence()
        {
            _activePresence = null;
            _client?.SetPresence(_defaultPresence);
        }

        public void Dispose()
        {
            _stop();
        }

        public void PostInject()
        {
            _configurationManager.RegisterCVar("discord.enabled", true, onValueChanged: newValue =>
            {
                if (!_initialized)
                {
                    return;
                }

                if (newValue)
                {
                    if (_client == null || _client.Disposed)
                    {
                        _start();
                    }
                }
                else
                {
                    _stop();
                }
            });
        }

        private sealed class NativeLogger : ILogger
        {
            private readonly ISawmill _sawmill;

            public NativeLogger(ISawmill sawmill)
            {
                _sawmill = sawmill;
            }

            public void Trace(string message, params object[] args)
            {
                if (Level > LogLevel.Trace)
                {
                    return;
                }

                _sawmill.Debug(message, args);
            }

            public void Info(string message, params object[] args)
            {
                if (Level > LogLevel.Info)
                {
                    return;
                }

                _sawmill.Info(message, args);
            }

            public void Warning(string message, params object[] args)
            {
                if (Level > LogLevel.Warning)
                {
                    return;
                }

                _sawmill.Warning(message, args);
            }

            public void Error(string message, params object[] args)
            {
                _sawmill.Error(message, args);
            }

            public LogLevel Level { get; set; } = LogLevel.Info;
        }
    }
}
