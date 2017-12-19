using Godot;
using SS14.Client.Godot;
using SS14.Client.Log;
using SS14.Shared.Configuration;
using SS14.Shared.ContentPack;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Log;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Interfaces.Physics;
using SS14.Shared.Interfaces.Timing;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Network;
using SS14.Shared.Physics;
using SS14.Shared.Prototypes;
using SS14.Shared.Timing;

namespace SS14.Client
{
    public class EntryPoint : ClientEntryPoint
    {
        public override void Main()
        {
            RegisterIoC();
            Logger.Debug("IoC Initialized!");
        }

        private void RegisterIoC()
        {
            IoCManager.Register<ILogManager, GodotLogManager>();
            IoCManager.BuildGraph();
        }
    }
}
