using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServerInterfaces;

namespace SGO
{
    public class BasicActorComponent : GameObjectComponent
    {
        IPlayerSession playerSession;

        public BasicActorComponent()
        {
            family = SS3D_shared.GO.ComponentFamily.Actor;
        }

        public override void SetParameter(ComponentParameter parameter)
        {
            switch (parameter.MemberName)
            {
                case "playersession":
                    if (parameter.ParameterType == typeof(IPlayerSession))
                        playerSession = (IPlayerSession)parameter.Parameter;
                    break;
            }
        }
    }
}
