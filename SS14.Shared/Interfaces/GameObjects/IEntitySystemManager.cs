using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SS14.Shared.IoC;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.System;

namespace SS14.Shared.Interfaces.GameObjects
{
    /// <summary>
    /// Like SS13's master controller.
    /// Periodically ticks <see cref="IEntitySystem"/> instances.
    /// </summary>
    public interface IEntitySystemManager : IIoCInterface
    {
        void RegisterMessageType<T>(IEntitySystem regSystem) where T : EntitySystemMessage;
        T GetEntitySystem<T>() where T : IEntitySystem;
        void Initialize();
    }
}
