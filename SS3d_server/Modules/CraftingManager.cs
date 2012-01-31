using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Lidgren.Network;
using ServerServices;
using SGO;
using SS3D_shared;

using System.Timers;

namespace SS3D_Server.Modules
{
    [Serializable]
    public class CraftRecipes
    {
        const int _Version = 1;
        public List<CraftingEntry> List = new List<CraftingEntry>();
    }

    [Serializable]
    public class CraftingEntry
    {
        public List<string> components = new List<string>();
        public int secondsToCreate = 5;
        public string result = "NULL";
    }

    public struct CraftingTicket
    {
        public DateTime doneAt;
        public int sourceUid;
        public string component1;
        public string component2;
        public string result;
    }

    public sealed class CraftingManager
    {
        public CraftRecipes recipes = new CraftRecipes();
        private string craftingListFile;
        private NetServer netServer;
        private PlayerManager playerManager;

        List<CraftingTicket> craftingTickets = new List<CraftingTicket>(); 

        static readonly CraftingManager singleton = new CraftingManager();

        static CraftingManager()
        {
        }

        CraftingManager()
        {
        }

        public static CraftingManager Singleton
        {
            get
            {
                return singleton;
            }
        }

        public bool isValidRecipe(string compo1, string compo2)
        {
            if(getRecipe(compo1,compo2) != null) return true;
            else return false;
        }

        public void Update()
        {
            foreach (var craftingTicket in craftingTickets)
            {
                if(craftingTicket.doneAt.Subtract(DateTime.Now).Ticks <= 0)
                {
                    //Crafting done.
                }
            }
        }

        public CraftingEntry getRecipe(string compo1, string compo2)
        {
            foreach (CraftingEntry recipe in recipes.List)
            {
                List<string> required = recipe.components;
                if (required.Exists(x => x.ToLowerInvariant() == compo1.ToLowerInvariant())) required.Remove(required.First(x => x.ToLowerInvariant() == compo1.ToLowerInvariant()));
                if (required.Exists(x => x.ToLowerInvariant() == compo2.ToLowerInvariant())) required.Remove(required.First(x => x.ToLowerInvariant() == compo2.ToLowerInvariant()));
                if (!required.Any()) return recipe;
            }
            return null;
        }

        public void BeginCrafting(string compo1, string compo2, int sourceUid) //Check for components and remove.
        {
            if (!isValidRecipe(compo1, compo2)) return;
            CraftingEntry recipe = getRecipe(compo1, compo2);
            CraftingTicket newTicket = new CraftingTicket();
            if(recipe.components.Count < 2) return;

            newTicket.component1 = recipe.components[0];
            newTicket.component2 = recipe.components[1];
            newTicket.sourceUid = sourceUid;
            newTicket.result = recipe.result;
            newTicket.doneAt = DateTime.Now.AddSeconds(recipe.secondsToCreate);

            craftingTickets.Add(newTicket);

            NetOutgoingMessage startCraftMsg = SS3DNetServer.Singleton.CreateMessage(); //Not starcraft. sorry.
            startCraftMsg.Write((byte)NetMessage.PlayerUiMessage);
            startCraftMsg.Write((byte)UiManagerMessage.ComponentMessage);
            startCraftMsg.Write((byte)GuiComponentType.ComboGUI);
            startCraftMsg.Write((byte)ComboGuiMessage.ShowCraftBar);
            startCraftMsg.Write(recipe.secondsToCreate);
        }

        public void Initialize(string craftingListLoc, SS3DNetServer _netServer, PlayerManager _playerManager)
        {
            netServer = _netServer;
            playerManager = _playerManager;

            if (File.Exists(craftingListLoc))
            {
                System.Xml.Serialization.XmlSerializer ConfigLoader = new System.Xml.Serialization.XmlSerializer(typeof(CraftRecipes));
                StreamReader ConfigReader = File.OpenText(craftingListLoc);
                CraftRecipes _loaded = (CraftRecipes)ConfigLoader.Deserialize(ConfigReader);
                ConfigReader.Close();
                recipes = _loaded;
                craftingListFile = craftingListLoc;
                LogManager.Log("Crafting Recipes loaded. " + recipes.List.Count.ToString() + " recipe" + (recipes.List.Count != 1 ? "s." : "."));
            }
            else
            {
                if (LogManager.Singleton != null) LogManager.Log("No Recipes found. Creating Empty List (" + craftingListLoc + ")");
                recipes = new CraftRecipes();
                recipes.List.Add(new CraftingEntry());
                craftingListFile = craftingListLoc;
                Save();
            }
        }

        public void Save()
        {
            if (recipes == null)
                return;
            else
            {
                System.Xml.Serialization.XmlSerializer ConfigSaver = new System.Xml.Serialization.XmlSerializer(typeof(CraftRecipes));
                StreamWriter ConfigWriter = File.CreateText(craftingListFile);
                ConfigSaver.Serialize(ConfigWriter, recipes);
                ConfigWriter.Flush();
                ConfigWriter.Close();
            }
        }
    }
}
