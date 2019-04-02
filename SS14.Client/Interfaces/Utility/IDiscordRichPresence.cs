using System;

namespace SS14.Client.Interfaces.Utility
{
    public interface IDiscordRichPresence: IDisposable
    {
        void Connect();
        void Update(string serverName, string Username, string maxUser);
    }
}
