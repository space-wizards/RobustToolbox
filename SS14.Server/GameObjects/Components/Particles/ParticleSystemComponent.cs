using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using YamlDotNet.RepresentationModel;

namespace SS14.Server.GameObjects
{
    public class ParticleSystemComponent : Component, IParticleSystemComponent
    {
        public override string Name => "ParticleSystem";
        public override uint? NetID => NetIDs.PARTICLE_SYSTEM;

        private Dictionary<string, Boolean> emitters = new Dictionary<string, bool>();

        //Notes: The server doesn't actually care about whether the client can't find a particle system when we tell it to add it.
        //       The server is literally just a list of stuff we want the clients to use - if they can't then tough luck.
        //       It's not the most elegant Solution but short of passing around a whole bunch of messages to verify that everything worked
        //       there's not much else i can do. And quite frankly - it doesn't matter if the client cant find a particle system - it just wont show up - no error.
        // --- years later ---
        // You're retarded. An error is an error. TODO fix this.

        public override ComponentReplyMessage ReceiveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = base.ReceiveMessage(sender, type, list);

            if (sender == this)
                return reply;

            switch (type)
            {
                case ComponentMessageType.Activate:
                    HandleClickedInHand();
                    break;
            }

            return reply;
        }

        private void HandleClickedInHand() //TODO: This is really dumb. Change this !!!
        {
            foreach (KeyValuePair<string, bool> emitter in new Dictionary<string, bool>(emitters)) //to work around "collection modified" crap.
                emitters[emitter.Key] = !emitters[emitter.Key];
        }

        public override ComponentState GetComponentState()
        {
            return new ParticleSystemComponentState(emitters);
        }

        public void AddParticleSystem(string name, bool active)
        {
            if (!emitters.ContainsKey(name))
                emitters.Add(name, active);
        }

        public void RemoveParticleSystem(string name)
        {
            if (emitters.ContainsKey(name))
                emitters.Remove(name);
        }

        public void SetParticleSystemActive(string name, bool active)
        {
            if (emitters.ContainsKey(name))
                emitters[name] = active;
        }
    }
}
