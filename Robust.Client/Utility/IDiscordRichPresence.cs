using System;

namespace Robust.Client.Utility
{
    public interface IDiscordRichPresence: IDisposable
    {
        void Initialize();
        void Update(string serverName, string username, string maxUser);
        void ClearPresence();
    }
}
