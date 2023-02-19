using System;

namespace Robust.Client.Utility
{
    public interface IDiscordRichPresence: IDisposable
    {
        void Initialize();
        void Update(string serverName, string username, string maxUser, string Users);
        void ClearPresence();
    }
}
