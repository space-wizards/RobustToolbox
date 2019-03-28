using DiscordRPC;
using DiscordRPC.Message;
using DiscordRPC.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SS14.Client.Utility;

namespace SS14.Client.Utility
{
    class RichPresenceClient
    {
        DiscordRpcClient client;
        System.Timers.Timer timer;

        private void BasicClient()
        {
            //Create a new client
            var client = new DiscordRpcClient("560482798364917789");

            //Create some events so we know things are happening
            //Create a timer that will regularly call invoke
            var timer = new System.Timers.Timer(150);
            timer.Elapsed += (sender, evt) => { client.Invoke(); };
            timer.Start();

            //Connect
            client.Initialize();

            //Send a presence. Do this as many times as you want
            client.SetPresence(new RichPresence()
            {
                Details = "A Basic Example",
                State = "In Game",
                Timestamps = Timestamps.FromTimeSpan(10)
            });       
        }

        public void Free()
        {
            client.Dispose();
            timer.Dispose();
        }

        public RichPresenceClient()
        {
            BasicClient();

        }
    }
}
