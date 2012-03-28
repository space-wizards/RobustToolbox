using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Lidgren.Network;
using SS13_Shared.GO;
using ServerServices;
using SGO;
using SS13_Shared;

using System.Timers;

namespace SS13_Server.Modules
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
        public Entity sourceEntity;
        public Entity component1;
        public Entity component2;
        public NetConnection sourceConnection;
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

        public void removeTicketByConnection(NetConnection connection)
        {
            var ticket = craftingTickets.First(x => x.sourceConnection == connection);
            craftingTickets.Remove(ticket);
        }

        public bool isValidRecipe(string compo1, string compo2)
        {
            if(getRecipe(compo1,compo2) != null) return true;
            else return false;
        }

        public void HandleNetMessage(NetIncomingMessage msg)
        {
            switch((CraftMessage)msg.ReadByte())
            {
                case CraftMessage.StartCraft:
                    HandleCraftRequest(msg);
                    break;
            }
        }

        public void HandleCraftRequest(NetIncomingMessage msg)
        {
            int compo1Uid = msg.ReadInt32();
            int compo2Uid = msg.ReadInt32();

            Entity compo1Ent = EntityManager.Singleton.GetEntity(compo1Uid);
            Entity compo2Ent = EntityManager.Singleton.GetEntity(compo2Uid);

            foreach (var ticket in craftingTickets)
            {
                if(ticket.sourceConnection == msg.SenderConnection && ticket.sourceEntity == playerManager.GetSessionByConnection(msg.SenderConnection).attachedEntity)
                {
                    sendAlreadyCrafting(msg.SenderConnection);
                    return;
                }
            }

            if (compo1Ent == null || compo2Ent == null) return;

            if(isValidRecipe(compo1Ent.template.Name, compo2Ent.template.Name))
            {
                if (hasFreeInventorySlots(playerManager.GetSessionByConnection(msg.SenderConnection).attachedEntity))
                    BeginCrafting(compo1Ent, compo2Ent, playerManager.GetSessionByConnection(msg.SenderConnection).attachedEntity, msg.SenderConnection);
                else
                    sendInventoryFull(msg.SenderConnection);
            }
            else
            {
                NetOutgoingMessage failCraftMsg = SS13NetServer.Singleton.CreateMessage();
                failCraftMsg.Write((byte)NetMessage.PlayerUiMessage);
                failCraftMsg.Write((byte)UiManagerMessage.ComponentMessage);
                failCraftMsg.Write((byte)GuiComponentType.ComboGui);
                failCraftMsg.Write((byte)ComboGuiMessage.CraftNoRecipe);
                netServer.SendMessage(failCraftMsg, msg.SenderConnection, NetDeliveryMethod.ReliableUnordered);
            }
        }

        private void sendCancelCraft(NetConnection connection)
        {
            NetOutgoingMessage cancelCraftMsg = SS13NetServer.Singleton.CreateMessage();
            cancelCraftMsg.Write((byte)NetMessage.PlayerUiMessage);
            cancelCraftMsg.Write((byte)UiManagerMessage.ComponentMessage);
            cancelCraftMsg.Write((byte)GuiComponentType.ComboGui);
            cancelCraftMsg.Write((byte)ComboGuiMessage.CancelCraftBar);
            netServer.SendMessage(cancelCraftMsg, connection, NetDeliveryMethod.ReliableUnordered);
            removeTicketByConnection(connection); //Better placement for this.
        }

        private void sendInventoryFull(NetConnection connection)
        {
            NetOutgoingMessage inventoyFullMsg = SS13NetServer.Singleton.CreateMessage();
            inventoyFullMsg.Write((byte)NetMessage.PlayerUiMessage);
            inventoyFullMsg.Write((byte)UiManagerMessage.ComponentMessage);
            inventoyFullMsg.Write((byte)GuiComponentType.ComboGui);
            inventoyFullMsg.Write((byte)ComboGuiMessage.CraftNeedInventorySpace);
            netServer.SendMessage(inventoyFullMsg, connection, NetDeliveryMethod.ReliableUnordered);
        }

        private void sendAlreadyCrafting(NetConnection connection)
        {
            NetOutgoingMessage busyMsg = SS13NetServer.Singleton.CreateMessage();
            busyMsg.Write((byte)NetMessage.PlayerUiMessage);
            busyMsg.Write((byte)UiManagerMessage.ComponentMessage);
            busyMsg.Write((byte)GuiComponentType.ComboGui);
            busyMsg.Write((byte)ComboGuiMessage.CraftAlreadyCrafting);
            netServer.SendMessage(busyMsg, connection, NetDeliveryMethod.ReliableUnordered);
        }

        private void sendCraftSuccess(NetConnection connection, Entity result, CraftingTicket ticket)
        {
            NetOutgoingMessage successMsg = SS13NetServer.Singleton.CreateMessage();
            successMsg.Write((byte)NetMessage.PlayerUiMessage);
            successMsg.Write((byte)UiManagerMessage.ComponentMessage);
            successMsg.Write((byte)GuiComponentType.ComboGui);
            successMsg.Write((byte)ComboGuiMessage.CraftSuccess);
            successMsg.Write((string)ticket.component1.template.Name);
            successMsg.Write((string)ticket.component1.Name);
            successMsg.Write((string)ticket.component2.template.Name);
            successMsg.Write((string)ticket.component2.Name);
            successMsg.Write((string)result.template.Name);
            successMsg.Write((string)result.Name);
            netServer.SendMessage(successMsg, connection, NetDeliveryMethod.ReliableUnordered);
            removeTicketByConnection(connection); //Better placement for this.
        }

        private void sendCraftMissing(NetConnection connection)
        {
            NetOutgoingMessage missingMsg = SS13NetServer.Singleton.CreateMessage();
            missingMsg.Write((byte)NetMessage.PlayerUiMessage);
            missingMsg.Write((byte)UiManagerMessage.ComponentMessage);
            missingMsg.Write((byte)GuiComponentType.ComboGui);
            missingMsg.Write((byte)ComboGuiMessage.CraftItemsMissing);
            netServer.SendMessage(missingMsg, connection, NetDeliveryMethod.ReliableUnordered);
            removeTicketByConnection(connection); //Better placement for this.
        }

        public void Update()
        {
            foreach (var craftingTicket in craftingTickets.ToArray())
            {
                if(craftingTicket.doneAt.Subtract(DateTime.Now).Ticks <= 0)
                {
                    if (hasFreeInventorySlots(playerManager.GetSessionByConnection(craftingTicket.sourceConnection).attachedEntity))
                        //sendCancelCraft(craftingTicket.sourceConnection); //Possible problem if they disconnect while crafting. FIX THIS!!!!!!!!!!!!

                        if (hasEntityInInventory(craftingTicket.sourceEntity, craftingTicket.component1) && hasEntityInInventory(craftingTicket.sourceEntity, craftingTicket.component2))
                        {
                            Entity newEnt = EntityManager.Singleton.SpawnEntity(craftingTicket.result);
                            sendCraftSuccess(craftingTicket.sourceConnection, newEnt, craftingTicket);
                            //craftingTicket.sourceEntity.SendMessage(this, ComponentMessageType.DisassociateEntity, null, craftingTicket.component1);
                            //craftingTicket.sourceEntity.SendMessage(this, ComponentMessageType.DisassociateEntity, null, craftingTicket.component2);
                            craftingTicket.sourceEntity.SendMessage(this, ComponentMessageType.InventoryAdd, newEnt);
                            EntityManager.Singleton.DeleteEntity(craftingTicket.component1); //This might be unsafe and MIGHt leave behind references. Gotta check that later.
                            EntityManager.Singleton.DeleteEntity(craftingTicket.component2);
                        }
                        else
                            sendCraftMissing(craftingTicket.sourceConnection);
                        //Create item and add it to inventory and also remove source items. Check if they still have source items.

                    else
                        sendInventoryFull(craftingTicket.sourceConnection);
                }
            }
        }

        private bool hasEntityInInventory(Entity container, Entity toSearch)
        {
            InventoryComponent compo = (InventoryComponent)container.GetComponent(ComponentFamily.Inventory);
            if (compo.containsEntity(toSearch)) return true;
            else return false;
        }

        private bool hasFreeInventorySlots(Entity entity)
        {
            InventoryComponent compo = (InventoryComponent) entity.GetComponent(ComponentFamily.Inventory);
            if (compo.containedEntities.Count >= compo.maxSlots) return false;
            else return true;
        }

        public CraftingEntry getRecipe(string compo1, string compo2)
        {
            foreach (CraftingEntry recipe in recipes.List)
            {
                List<string> required = new List<string>(recipe.components);
                if (required.Exists(x => x.ToLowerInvariant() == compo1.ToLowerInvariant())) required.Remove(required.First(x => x.ToLowerInvariant() == compo1.ToLowerInvariant()));
                if (required.Exists(x => x.ToLowerInvariant() == compo2.ToLowerInvariant())) required.Remove(required.First(x => x.ToLowerInvariant() == compo2.ToLowerInvariant()));
                if (!required.Any()) return recipe;
            }
            return null;
        }

        public void BeginCrafting(Entity compo1, Entity compo2, Entity source, NetConnection sourceConnection) //Check for components and remove.
        {
            if (!isValidRecipe(compo1.template.Name, compo2.template.Name)) return;
            CraftingEntry recipe = getRecipe(compo1.template.Name, compo2.template.Name);
            CraftingTicket newTicket = new CraftingTicket();
            if(recipe.components.Count < 2) return;

            newTicket.component1 = compo1;
            newTicket.component2 = compo2;
            newTicket.sourceEntity = source;
            newTicket.result = recipe.result;
            newTicket.doneAt = DateTime.Now.AddSeconds(recipe.secondsToCreate);
            newTicket.sourceConnection = sourceConnection;

            craftingTickets.Add(newTicket);

            NetOutgoingMessage startCraftMsg = SS13NetServer.Singleton.CreateMessage(); //Not starcraft. sorry.
            startCraftMsg.Write((byte)NetMessage.PlayerUiMessage);
            startCraftMsg.Write((byte)UiManagerMessage.ComponentMessage);
            startCraftMsg.Write((byte)GuiComponentType.ComboGui);
            startCraftMsg.Write((byte)ComboGuiMessage.ShowCraftBar);
            startCraftMsg.Write(recipe.secondsToCreate);

            netServer.SendMessage(startCraftMsg, sourceConnection, NetDeliveryMethod.ReliableUnordered);
        }

        public void Initialize(string craftingListLoc, SS13NetServer _netServer, PlayerManager _playerManager)
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
                CraftingEntry dummy = new CraftingEntry();
                dummy.components.Add("Null1");
                dummy.components.Add("Null2");
                recipes.List.Add(dummy);
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
