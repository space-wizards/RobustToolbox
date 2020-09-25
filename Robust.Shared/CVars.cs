using System;
using Robust.Shared.Configuration;

namespace Robust.Shared
{
    [CVarDefs]
    public abstract class CVars
    {
        protected CVars()
        {
            throw new InvalidOperationException("This class must not be instantiated");
        }

        public static readonly CVarDef<int> NetPort = CVarDef.Create("net.port", 1212, CVar.ARCHIVE);

        public static readonly CVarDef<int> NetSendBufferSize =
            CVarDef.Create("net.sendbuffersize", 131071, CVar.ARCHIVE);

        public static readonly CVarDef<int> NetReceiveBufferSize =
            CVarDef.Create("net.receivebuffersize", 131071, CVar.ARCHIVE);

        public static readonly CVarDef<bool> NetVerbose =
            CVarDef.Create("net.verbose", false);

        public static readonly CVarDef<string> NetServer =
            CVarDef.Create("net.server", "127.0.0.1", CVar.ARCHIVE | CVar.CLIENTONLY);

        public static readonly CVarDef<int> NetUpdateRate =
            CVarDef.Create("net.updaterate", 20, CVar.ARCHIVE | CVar.CLIENTONLY);

        public static readonly CVarDef<int> NetCmdRate =
            CVarDef.Create("net.cmdrate", 30, CVar.ARCHIVE | CVar.CLIENTONLY);

        public static readonly CVarDef<int> NetRate =
            CVarDef.Create("net.rate", 10240, CVar.ARCHIVE | CVar.REPLICATED | CVar.CLIENTONLY);

        // That's comma-separated, btw.
        public static readonly CVarDef<string> NetBindTo =
            CVarDef.Create("net.bindto", "0.0.0.0,::", CVar.ARCHIVE | CVar.SERVERONLY);

        public static readonly CVarDef<bool> NetDualStack =
            CVarDef.Create("net.dualstack", false, CVar.ARCHIVE | CVar.SERVERONLY);

        public static readonly CVarDef<int> GameMaxPlayers =
            CVarDef.Create("game.maxplayers", 32, CVar.ARCHIVE | CVar.SERVERONLY);

#if DEBUG
        public static readonly CVarDef<float> NetFakeLoss = CVarDef.Create("net.fakeloss", 0f, CVar.CHEAT);
        public static readonly CVarDef<float> NetFakeLagMin = CVarDef.Create("net.fakelagmin", 0f, CVar.CHEAT);
        public static readonly CVarDef<float> NetFakeLagRand = CVarDef.Create("net.fakelagrand", 0f, CVar.CHEAT);
        public static readonly CVarDef<float> NetFakeDuplicates = CVarDef.Create("net.fakeduplicates", 0f, CVar.CHEAT);
#endif
    }
}
