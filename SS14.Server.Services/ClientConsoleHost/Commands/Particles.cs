using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Server.Services.ClientConsoleHost.Commands
{
    class Particles
    {/*
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
}
