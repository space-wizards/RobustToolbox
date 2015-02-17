using Lidgren.Network;
using SS14.Server.Interfaces;
using SS14.Server.Interfaces.Atmos;
using SS14.Server.Interfaces.ClientConsoleHost;
using SS14.Server.Interfaces.GOC;
using SS14.Server.Interfaces.Map;
using SS14.Server.Interfaces.Network;
using SS14.Server.Interfaces.Player;
using SS14.Server.Services.Atmos;
using SS14.Server.Services.Tiles;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace SS14.Server.Services.ClientConsoleHost
{
    class ClientConsoleHost : IClientConsoleHost
    {
        public void ProcessCommand(string text, NetConnection sender)
        {
            var args = new List<string>();

            CommandParsing.ParseArguments(text, args);

            string command = args[0];

            Vector2 position;
            Entity player;

            var playerMgr = IoCManager.Resolve<IPlayerManager>();

            player = playerMgr.GetSessionByConnection(sender).attachedEntity;

            var map = IoCManager.Resolve<IMapManager>();
            switch (command)
            {
                case "addparticles":
                    if (args.Count >= 3)
                    {
                        var _serverMain = IoCManager.Resolve<ISS14Server>();
                        Entity target = null;
                        if (args[1].ToLowerInvariant() == "player")
                            target = player;
                        else
                        {
                            int entUid = int.Parse(args[1]);
                            target = _serverMain.EntityManager.GetEntity(entUid);
                        }

                        if (target != null)
                        {
                            if (target.HasComponent(ComponentFamily.Particles))
                            {
                                IParticleSystemComponent compo = (IParticleSystemComponent)target.GetComponent(ComponentFamily.Particles);
                                compo.AddParticleSystem(args[2], true);
                            }
                            else
                            {
                                var compo = (IParticleSystemComponent)_serverMain.EntityManager.ComponentFactory.GetComponent("ParticleSystemComponent");
                                target.AddComponent(ComponentFamily.Particles, compo);
                                compo.AddParticleSystem(args[2], true);
                                //Can't find a way to add clientside compo from here.
                            }
                        }
                    }
                    break;
                case "removeparticles":
                    if (args.Count >= 3)
                    {
                        var _serverMain = IoCManager.Resolve<ISS14Server>();
                        Entity target = null;
                        if (args[1].ToLowerInvariant() == "player")
                            target = player;
                        else
                        {
                            int entUid = int.Parse(args[1]);
                            target = _serverMain.EntityManager.GetEntity(entUid);
                        }

                        if (target != null)
                        {
                            if (target.HasComponent(ComponentFamily.Particles))
                            {
                                IParticleSystemComponent compo = (IParticleSystemComponent)target.GetComponent(ComponentFamily.Particles);
                                compo.RemoveParticleSystem(args[2]);
                            }
                        }
                    }
                    break;
                case "addgas":
                    if (args.Count > 1 && Convert.ToDouble(args[1]) > 0)
                    {
                        if (player != null)
                        {
                            double amount = Convert.ToDouble(args[1]);
                            var t =
                                map.GetFloorAt(
                                    player.GetComponent<ITransformComponent>(ComponentFamily.Transform).Position) as
                                Tile;
                            if (t != null)
                                t.GasCell.AddGas((float) amount, GasType.Toxin);
                            SendConsoleReply(amount.ToString() + " Gas added.", sender);
                        }
                    }
                    break;
                case "heatgas":
                    if (args.Count > 1 && Convert.ToDouble(args[1]) > 0)
                    {
                        if (player != null)
                        {
                            double amount = Convert.ToDouble(args[1]);
                            var t =
                                map.GetFloorAt(
                                    player.GetComponent<ITransformComponent>(ComponentFamily.Transform).Position) as
                                Tile;
                            if (t != null)
                                t.GasCell.AddGas((float) amount, GasType.Toxin);
                            SendConsoleReply(amount.ToString() + " Gas added.", sender);
                        }
                    }
                    break;
                case "atmosreport":
                    IoCManager.Resolve<IAtmosManager>().TotalAtmosReport();
                    break;
                case "tpvreport": // Reports on temp / pressure
                    if (player != null)
                    {
                        var ti =
                            (Tile)
                            map.GetFloorAt(player.GetComponent<ITransformComponent>(ComponentFamily.Transform).Position);
                        if (ti == null)
                            break;
                        GasCell ce = ti.gasCell;
                        SendConsoleReply("T/P/V: " + ce.GasMixture.Temperature.ToString() + " / " + ce.GasMixture.Pressure.ToString() + " / " + ce.GasVelocity.ToString(), sender);   
                        //var chatMgr = IoCManager.Resolve<IChatManager>();
                        //chatMgr.SendChatMessage(ChatChannel.Default,
                        //                        "T/P/V: " + ce.GasMixture.Temperature.ToString() + " / " +
                        //                        ce.GasMixture.Pressure.ToString() + " / " + ce.GasVelocity.ToString(),
                        //                        "TempCheck",
                        //                        0);
                    }
                    break;
                case "gasreport":
                    if (player != null)
                    {
                        var tile = map.GetFloorAt(player.GetComponent<ITransformComponent>(ComponentFamily.Transform).Position) as Tile;
                        if (tile == null)
                            break;
                        GasCell c = tile.gasCell;
                        for (int i = 0; i < c.GasMixture.gasses.Length; i++)
                        {
                            SendConsoleReply(((GasType) i).ToString() + ": " +c.GasMixture.gasses[i].ToString(CultureInfo.InvariantCulture) + " m", sender); 
                            //var chatMgr = IoCManager.Resolve<IChatManager>();
                            //chatMgr.SendChatMessage(ChatChannel.Default,
                            //                        ((GasType) i).ToString() + ": " +
                            //                        c.GasMixture.gasses[i].ToString(CultureInfo.InvariantCulture) + " m",
                            //                        "GasReport", 0);
                        }
                    }
                    break;
                case "everyonesondrugs":
                    foreach (IPlayerSession playerfordrugs in IoCManager.Resolve<IPlayerManager>().GetAllPlayers())
                    {
                        playerfordrugs.AddPostProcessingEffect(PostProcessingEffectType.Acid, 60);
                        SendConsoleReply("Okay then.", sender);
                    }
                    break;
                default:
                    string message = "Command '" + command + "' not recognized.";
                    SendConsoleReply(message, sender);
                    break;
            }
        }

        public void SendConsoleReply(string text, NetConnection target)
        {
            var netMgr = IoCManager.Resolve<ISS14NetServer>();
            NetOutgoingMessage replyMsg = netMgr.CreateMessage();
            replyMsg.Write((byte)NetMessage.ConsoleCommandReply);
            replyMsg.Write(text);
            netMgr.SendMessage(replyMsg, target, NetDeliveryMethod.ReliableUnordered);
        }
    }
}
