using SS14.Shared.Log;
using System;
using System.Collections.Generic;

namespace SS14.Client.State.States
{
    // OH GOD.
    public sealed partial class GameScreen : State
    {
        public GameScreen(IDictionary<Type, object> managers) : base(managers)
        {
        }

        public override void InitializeGUI()
        {
            //throw new System.NotImplementedException();
        }

        public override void Shutdown()
        {
            //throw new System.NotImplementedException();
        }

        public override void Startup()
        {
            Logger.Debug("Oh no.");
            //throw new System.NotImplementedException();
        }
    }
}
