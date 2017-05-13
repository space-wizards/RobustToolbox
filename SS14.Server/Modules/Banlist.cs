using SS14.Shared;
using SS14.Shared.Log;
using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace SS14.Server.Modules
{
    public sealed class BanlistMgr
    {
        private static readonly BanlistMgr singleton = new BanlistMgr();
        private string banListFile;
        public Banlist banlist;

        static BanlistMgr()
        {
        }

        private BanlistMgr()
        {
        }

        public static BanlistMgr Singleton
        {
            get { return singleton; }
        }

        public Boolean IsBanned(string ip)
        {
            BanEntry ban = (from BanEntry entry in banlist.List
                            where entry.ip.Equals(ip)
                            select entry).FirstOrDefault();

            if (ban != null) return true;

            return false;
        }

        public BanEntry GetBanByIp(string ip)
        {
            BanEntry ban = (from BanEntry entry in banlist.List
                            where entry.ip.Equals(ip)
                            select entry).FirstOrDefault();

            if (ban != null) return ban;

            return null;
        }

        public void RemoveBanByIp(string ip)
        {
            BanEntry ban = (from BanEntry entry in banlist.List
                            where entry.ip.Equals(ip)
                            select entry).FirstOrDefault();

            if (ban != null)
            {
                LogManager.Log("Ban Removed: " + ban.ip);
                banlist.List.Remove(ban);
                Save();
            }
        }

        public void AddBan(string ip, string reason, TimeSpan time)
        {
            var newEntry = new BanEntry();
            newEntry.ip = ip;
            newEntry.reason = reason;
            newEntry.bannedAt = DateTime.Now;
            newEntry.expiresAt = DateTime.Now.Add(time);
            newEntry.tempBan = true;
            banlist.List.Add(newEntry);
            LogManager.Log("Ban Added: " + ip);
            Save();
        }

        public void AddBan(string ip, string reason)
        {
            var newEntry = new BanEntry();
            newEntry.ip = ip;
            newEntry.reason = reason;
            newEntry.bannedAt = DateTime.Now;
            newEntry.tempBan = false;
            banlist.List.Add(newEntry);
            LogManager.Log("Ban Added: " + ip);
            Save();
        }

        public void Initialize(string BanListLoc)
        {
            if (File.Exists(BanListLoc))
            {
                var ConfigLoader = new XmlSerializer(typeof (Banlist));
                StreamReader ConfigReader = File.OpenText(BanListLoc);
                var Config = (Banlist) ConfigLoader.Deserialize(ConfigReader);
                ConfigReader.Close();
                banlist = Config;
                banListFile = BanListLoc;
                LogManager.Log("Banlist loaded. " + banlist.List.Count.ToString() + " ban" +
                               (banlist.List.Count != 1 ? "s." : "."));
            }
            else
            {
                if (LogManager.Singleton != null)
                    LogManager.Log("No Banlist found. Creating Empty List (" + BanListLoc + ")");
                banlist = new Banlist();
                banlist.List.Add(new BanEntry());
                banListFile = BanListLoc;
                Save();
            }
        }

        public void Save()
        {
            if (banlist == null)
                return;
            else
            {
                var ConfigSaver = new XmlSerializer(typeof (Banlist));
                StreamWriter ConfigWriter = File.CreateText(banListFile);
                ConfigSaver.Serialize(ConfigWriter, banlist);
                ConfigWriter.Flush();
                ConfigWriter.Close();
            }
        }
    }
}