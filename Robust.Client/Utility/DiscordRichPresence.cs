using DiscordRPC;
using DiscordRPC.Logging;
using Robust.Client.Interfaces.Utility;
using Robust.Shared;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.IoC;

namespace Robust.Client.Utility
{
    internal sealed class DiscordRichPresence : IDiscordRichPresence
    {
        private static readonly RichPresence _defaultPresence = new()
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

        private RichPresence? _activePresence;

        private DiscordRpcClient? _client;

        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly ILogManager _logManager = default!;

        private bool _initialized;

        public void Initialize()
        {
            _configurationManager.OnValueChanged(CVars.DiscordEnabled, newValue =>
            {
                if (!_initialized)
                {
                    return;
                }

                if (newValue)
                {
                    if (_client == null || _client.IsDisposed)
                    {
                        _start();
                    }
                }
                else
                {
                    _stop();
                }
            });
            
            if (_configurationManager.GetCVar(CVars.DiscordEnabled))
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

        private sealed class NativeLogger : ILogger
        {
            private readonly ISawmill _sawmill;
            /// <summary>
            /// This library keeps trying to connect all the time and spams the client with error messages.
            /// To mitigate this annoyance, this variable keeps track of whether a connection was established
            /// (or whether it's the first time it's being attempted) so that we can print the error only when it's useful.
            /// </summary>
            private bool _successfullyConnected = true;

            public NativeLogger(ISawmill sawmill)
            {
                _sawmill = sawmill;
            }

            public void Trace(string message, params object[] args)
            {
                if (message == "Setting the connection state to {0}" && args.Length > 0 && (string)args[0] == "CONNECTED")
                {
                    _successfullyConnected = true;
                }
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

                if (message == "Tried to close a already closed pipe.")
                {
                    if (_successfullyConnected)
                    {
                        _successfullyConnected = false;
                    }
                    else
                    {
                        return;
                    }
                }

                _sawmill.Warning(message, args);
            }

            public void Error(string message, params object[] args)
            {
                if (message.StartsWith("Failed connection to") || message == "Failed to connect for some reason.")
                {
                    if (_successfullyConnected)
                    {
                        _successfullyConnected = false;
                    }
                    else
                    {
                        return;
                    }
                }

                _sawmill.Error(message, args);
            }

            public LogLevel Level { get; set; } = LogLevel.Info;
        }
    }
}
