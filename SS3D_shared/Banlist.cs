using System;
using System.Collections.Generic;

namespace SS13_Shared
{
    [Serializable]
    public class Banlist
    {
        private const int _Version = 1;
        public List<BanEntry> List = new List<BanEntry>();
    }

    [Serializable]
    public class BanEntry
    {
        public DateTime bannedAt;
        public DateTime expiresAt;
        public string ip = "0.0.0.0";
        public string reason = "Empty Reason";
        public Boolean tempBan;
    }
}