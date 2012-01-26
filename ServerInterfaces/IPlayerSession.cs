using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using SS3D_shared;

namespace ServerInterfaces
{
    public interface IPlayerSession
    {
        NetConnection ConnectedClient { get; }
        BodyPart TargetedArea { get; }
    }
}
