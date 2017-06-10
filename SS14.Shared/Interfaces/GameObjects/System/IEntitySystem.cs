using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SS14.Shared.IoC;
using SS14.Shared.GameObjects;

namespace SS14.Shared.Interfaces.GameObjects.System
{
    /// <summary>
    /// Entity systems are similar to TGstation13 subsystems.
    /// They have a set of entities to run over and run every once in a while.
    /// They get managed by an <see cref="IEntitySystemManager"/>.
    /// </summary>
    public interface IEntitySystem : IIoCInterface
    {
        void RegisterMessageTypes();

        void SubscribeEvents();

        void Initialize();

        void Shutdown();

        void HandleNetMessage(EntitySystemMessage sysMsg);

        void Update(float frameTime);
    }
}
