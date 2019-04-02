using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordRPC;

namespace SS14.Client.Utility
{
    class DiscordRichPresence : IDisposable
    {
        private DiscordRpcClient _client;

        private readonly DiscordRPC.Logging.LogLevel logLevel = DiscordRPC.Logging.LogLevel.Info;

        private readonly RichPresence _presence = new RichPresence()
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

        public DiscordRichPresence()
        {
            // Create the client
            _client = new DiscordRpcClient("560499552273170473")
            {
                Logger = new DiscordRPC.Logging.ConsoleLogger(logLevel, true)
            };
            // == Subscribe to some events
            _client.OnReady += (sender, msg) =>
            {
                //Create some events so we know things are happening
                System.Console.WriteLine("Connected to discord with user {0}", msg.User.Username);
            };

            _client.OnPresenceUpdate += (sender, msg) =>
            {
                //The presence has updated
                System.Console.WriteLine("Presence has been updated! ");
            };

            // == Initialize
            _client.Initialize();

            // == Set the presence
            _client.SetPresence(_presence);
        }

        public void Update()
        {
            //TODO: Update presence with data
        }

        public void Dispose()
        {
            _client.Dispose();
        }

    }
}
