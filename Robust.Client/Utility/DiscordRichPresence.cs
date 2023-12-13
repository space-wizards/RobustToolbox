using System;
using System.Text;
using DiscordRPC;
using DiscordRPC.Logging;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using LogLevel = DiscordRPC.Logging.LogLevel;

namespace Robust.Client.Utility
{
    internal sealed class DiscordRichPresence : IDiscordRichPresence
    {
        private static RichPresence _defaultPresence = new() {};

        private RichPresence? _activePresence;

        private DiscordRpcClient? _client;

        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly ILogManager _logManager = default!;
        [Dependency] private readonly ILocalizationManager _loc = default!;

        private bool _initialized;

        public void Initialize()
        {
            var state = _loc.GetString("discord-rpc-in-main-menu");
            var largeImageKey = _configurationManager.GetCVar(CVars.DiscordRichPresenceSecondIconId);
            var largeImageText = _loc.GetString("discord-rpc-in-main-menu-logo-text");

            _defaultPresence = new()
            {
                State = Truncate(state, 128),
                Assets = new Assets
                {
                    LargeImageKey = Truncate(largeImageKey, 32),
                    LargeImageText = Truncate(largeImageText, 128),
                }
            };
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

        public void Update(string serverName, string username, string maxUsers, string users)
        {
            if (_client == null)
                return;

            try
            {
                var details = _loc.GetString("discord-rpc-on-server", ("servername", serverName));
                var state = _loc.GetString("discord-rpc-players", ("players", users), ("maxplayers", maxUsers));
                var largeImageText = _loc.GetString("discord-rpc-character", ("username", username));
                var largeImageKey = _configurationManager.GetCVar(CVars.DiscordRichPresenceMainIconId);
                var smallImageKey = _configurationManager.GetCVar(CVars.DiscordRichPresenceSecondIconId);

                // Strings are limited by byte count. See the setters in RichPresence. Hence the truncate calls.
                _activePresence = new RichPresence
                {
                    Details = Truncate(details, 128),
                    State = Truncate(state, 128),
                    Assets = new Assets
                    {
                        LargeImageKey = Truncate(largeImageKey, 32),
                        LargeImageText = Truncate(largeImageText, 128),
                        SmallImageKey = Truncate(smallImageKey, 32)
                    }
                };
                _client.SetPresence(_activePresence);
            }
            catch (Exception ex)
            {
                _client.Logger.Error($"Caught exception while updating discord rich presence. Exception:\n{ex}");
            }
        }

        private string Truncate(string value, int bytes, string postfix = "...")
            => Truncate(value, bytes, postfix, Encoding.UTF8);

        /// <summary>
        /// Truncate strings down to some minimum byte count. If the string gets truncated, it will have the postfix appended.
        /// </summary>
        private string Truncate(string value, int bytes, string postfix, Encoding encoding)
        {
            value = value.Trim();
            var output = value;

            // Theres probably a better way of doing this, but I don't know how.
            // If this wasn't a crude hack this function should
            while (encoding.GetByteCount(output) > bytes)
            {
                if (value.Length == 0)
                    return string.Empty;

                value = value.Substring(0, value.Length - 1);
                output = value + postfix;
            }

            return output;
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
