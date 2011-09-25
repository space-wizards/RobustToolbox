using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3D_shared
{
    [Serializable]
    public class Banlist
    {
        const int _Version = 1;
        public List<BanEntry> List = new List<BanEntry>();
    }

    [Serializable]
    public class BanEntry
    {
        public DateTime bannedAt;
        public DateTime expiresAt;
        public Boolean tempBan;
        public string ip = "0.0.0.0";
        public string reason = "Empty Reason";
    }
}
