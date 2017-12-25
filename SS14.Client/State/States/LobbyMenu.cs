using SS14.Client.Interfaces.Player;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using System;
using System.Collections.Generic;

namespace SS14.Client.State.States
{
    public class Lobby : State
    {
        public Lobby(IDictionary<Type, object> managers)
            : base(managers) { }

        public override void InitializeGUI()
        {
            // throw new System.NotImplementedException();
        }

        public override void Shutdown()
        {
            // throw new System.NotImplementedException();
        }

        public override void Startup()
        {
            IoCManager.Resolve<IPlayerManager>().SendVerb("joingame", 0);
        }
    }
}
