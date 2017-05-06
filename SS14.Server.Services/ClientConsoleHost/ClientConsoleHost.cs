using Lidgren.Network;
using SFML.System;
using SS14.Server.Interfaces;
using SS14.Server.Interfaces.ClientConsoleHost;
using SS14.Server.Interfaces.GOC;
using SS14.Server.Interfaces.Map;
using SS14.Server.Interfaces.Network;
using SS14.Server.Interfaces.Player;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using SS14.Shared.IoC;
using SS14.Shared.Utility;
using System.Collections.Generic;
using System;

namespace SS14.Server.Services.ClientConsoleHost
{
	class ClientConsoleHost : IClientConsoleHost
	{
		public void HandleRegistrationRequest(NetConnection senderConnection)
		{
			// TODO, send command names, descriptions and help back to client so client-side help commands can show it.
		}

		public void ProcessCommand(string text, NetConnection sender)
		{
			var args = new List<string>();

			CommandParsing.ParseArguments(text, args);

			if (args.Count == 0)
			{
				return;
			}
			string command = args[0];

			SendConsoleReply(string.Format("Command '{0}' not recognized.", command), sender);

			// TODO: Server side IClientCommand handling.

			/*
			Vector2f position;
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

				default:
					string message = "Command '" + command + "' not recognized.";
					SendConsoleReply(message, sender);
					break;
			}
			*/
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
