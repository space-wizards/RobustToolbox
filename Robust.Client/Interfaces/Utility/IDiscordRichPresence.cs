using System;

namespace Robust.Client.Interfaces.Utility
{
    public interface IDiscordRichPresence: IDisposable
    {
        void Connect();
        void Update(string serverName, string Username, string maxUser);
        void Restore();
    }
}
