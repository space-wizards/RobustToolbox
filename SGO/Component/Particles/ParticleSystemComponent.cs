using System;
using System.Collections.Generic;
using System.Drawing;
using System.Xml.Linq;
using GameObject;
using SS13.IoC;
using SS13_Shared;
using SS13_Shared.GO;
using SS13_Shared.GO.Component.Light;
using SS13_Shared.GO.Component.Particles;
using ServerInterfaces.Chat;

namespace SGO
{
    public class ParticleSystemComponent : Component
    {
        private Dictionary<string, Boolean> emitters = new Dictionary<string, bool>();

        //Notes: The server doesn't actually care about whether the client can't find a particle system when we tell it to add it.
        //       The server is literally just a list of stuff we wan't the clients to use - if they can't then tough luck.
        //       It's not the most elegant Solution but short of passing around a whole bunch of messages to verify that everything worked
        //       there's not much else i can do. And quite frankly - it doesn't matter if the client cant find a particle system - it just wont show up - no error.

        public ParticleSystemComponent()
        {
            Family = ComponentFamily.Particles;
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = base.RecieveMessage(sender, type, list);

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

        public override void HandleExtendedParameters(XElement extendedParameters)
        {
            foreach (XElement param in extendedParameters.DescendantNodes())
            {
                if(param.Name == "ParticleSystem")
                    if (param.Attribute("name") != null)
                    {
                        if (!emitters.ContainsKey(param.Attribute("name").Value))
                        {
                            if (param.Attribute("active") != null)
                            {
                                emitters.Add(param.Attribute("name").Value, bool.Parse(param.Attribute("active").Value));
                            }
                            else
                            {
                                emitters.Add(param.Attribute("name").Value, true);
                            }
                        }
                    }
            }
        }
    }
}