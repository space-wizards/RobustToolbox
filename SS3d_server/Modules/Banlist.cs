using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Lidgren.Network;

using SS3D_shared;

namespace SS3D_Server.Modules
{
    public sealed class BanlistMgr
    {
        public Banlist banlist;
        private string banListFile;

        static readonly BanlistMgr singleton = new BanlistMgr();

        static BanlistMgr()
        {
        }

        BanlistMgr()
        {
        }

        public static BanlistMgr Singleton
        {
            get
            {
                return singleton;
            }
        }

        public Boolean IsBanned(string ip)
        {
            var ban = (from BanEntry entry in banlist.List
                       where entry.ip.Equals(ip)
                       select entry).FirstOrDefault();

            if (ban != null) return true;

            return false;
        }

        public BanEntry GetBanByIp(string ip)
        {
            var ban = (from BanEntry entry in banlist.List
                       where entry.ip.Equals(ip)
                       select entry).FirstOrDefault();

            if (ban != null) return ban;

            return null;
        }

        public void RemoveBanByIp(string ip)
        {
            var ban = (from BanEntry entry in banlist.List
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
            BanEntry newEntry = new BanEntry();
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
            BanEntry newEntry = new BanEntry();
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
                System.Xml.Serialization.XmlSerializer ConfigLoader = new System.Xml.Serialization.XmlSerializer(typeof(Banlist));
                StreamReader ConfigReader = File.OpenText(BanListLoc);
                Banlist Config = (Banlist)ConfigLoader.Deserialize(ConfigReader);
                ConfigReader.Close();
                banlist = Config;
                banListFile = BanListLoc;
                LogManager.Log("Banlist loaded. "+banlist.List.Count.ToString()+" ban" + (banlist.List.Count != 1 ? "s." : "."));
            }
            else
            {
                if (LogManager.Singleton != null) LogManager.Log("No Banlist found. Creating Empty List (" + BanListLoc + ")");
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
                System.Xml.Serialization.XmlSerializer ConfigSaver = new System.Xml.Serialization.XmlSerializer(typeof(Banlist));
                StreamWriter ConfigWriter = File.CreateText(banListFile);
                ConfigSaver.Serialize(ConfigWriter, banlist);
                ConfigWriter.Flush();
                ConfigWriter.Close();
            }
        }
    }
}
