using DiscordRPC;
using SS14.Client.Interfaces.Utility;
using SS14.Shared.Log;

namespace SS14.Client.Utility
{
    class DiscordRichPresence : IDiscordRichPresence
    {
        private DiscordRpcClient _client;

        private readonly DiscordRPC.Logging.LogLevel logLevel = DiscordRPC.Logging.LogLevel.Info;

        private RichPresence _presence = new RichPresence()
        {
            Details = "devstation",
            State = "Testing Rich Presence",
            Assets = new Assets()
            {
                LargeImageKey = "devstation",
                LargeImageText = "I think coolsville SUCKS",
                SmallImageKey = "logo"
            },
            Timestamps = Timestamps.FromTimeSpan(10),
        };

        public void Connect()
        {
            // Create the client
            _client = new DiscordRpcClient("560499552273170473")
            {
                Logger = new DiscordRPC.Logging.ConsoleLogger(logLevel, true)
            };
            // == Subscribe to some events
            _client.OnReady += (sender, msg) =>
            {
                Logger.Info("Connected to discord with user {0}", msg.User.Username);
            };

            _client.OnPresenceUpdate += (sender, msg) =>
            {
                Logger.Info("Presence has been updated! ");
            };

            // == Initialize
            _client.Initialize();

            // == Set the presence
            _client.SetPresence(_presence);
        }

        public void Update(string serverName, string Username, string maxUser)
        {
            //TODO: tests
            _presence.Details = serverName;
            _presence.State = Username;
            _presence.Assets.LargeImageText = Username;
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

    }
}
