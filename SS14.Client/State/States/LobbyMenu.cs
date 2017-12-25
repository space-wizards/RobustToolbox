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
            Logger.Debug("We connected!");
            // throw new System.NotImplementedException();
        }

        public override void Shutdown()
        {
            // throw new System.NotImplementedException();
        }

        public override void Startup()
        {
            // throw new System.NotImplementedException();
        }
    }
}
