using Lidgren.Network;
using SFML.System;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using System.Collections.Generic;

namespace SS14.Server.Interfaces.GameObjects
{
    public interface IServerEntityManager : IEntityManager, IIoCInterface
    {
        void Initialize();
        void SaveEntities();
        IEntity SpawnEntity(string template, int uid = -1);
        IEntity SpawnEntityAt(string entityTemplateName, Vector2f vector2);
        List<EntityState> GetEntityStates();
    }
}
