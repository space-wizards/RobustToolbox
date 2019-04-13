using System;

namespace SS14.Client.Interfaces.Utility
{
    public interface IDiscordRichPresence: IDisposable
    {
        void Initialize();
        void Update(string serverName, string username, string maxUser);
        void ClearPresence();
    }
}
