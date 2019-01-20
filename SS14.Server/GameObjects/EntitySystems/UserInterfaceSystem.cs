using System.Collections.Generic;
using SS14.Server.GameObjects.Components.UserInterface;
using SS14.Server.Interfaces.Player;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Systems;
using SS14.Shared.Interfaces.GameObjects.Components;

namespace SS14.Server.GameObjects.EntitySystems
{
    internal class UserInterfaceSystem : EntitySystem
    {
        private const float MaxWindowRange = 2;
        private const float MaxWindowRangeSquared = MaxWindowRange * MaxWindowRange;

        private readonly List<IPlayerSession> _sessionCache = new List<IPlayerSession>();

        /// <inheritdoc />
        public override void Initialize()
        {
            EntityQuery = new TypeEntityQuery(typeof(ServerUserInterfaceComponent));
        }

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            foreach (var entity in RelevantEntities)
            {
                var uiComp = entity.GetComponent<ServerUserInterfaceComponent>();

                CheckRange(entity.Transform, uiComp);
            }
        }

        /// <summary>
        ///     Verify that the subscribed clients are still in range of the entity.
        /// </summary>
        /// <param name="transformComp">Transform Component of the entity being checked.</param>
        /// <param name="uiComp">UserInterface Component of entity being checked.</param>
        private void CheckRange(ITransformComponent transformComp, ServerUserInterfaceComponent uiComp)
        {
            foreach (var ui in uiComp.Interfaces)
            {
                // We have to cache the set of sessions because Unsubscribe modifies the original.
                _sessionCache.Clear();
                _sessionCache.AddRange(ui.SubscribedSessions);

                if (_sessionCache.Count == 0)
                    continue;

                var uiPos = transformComp.WorldPosition;
                var uiMap = transformComp.MapID;

                foreach (var session in _sessionCache)
                {
                    var attachedEntity = session.AttachedEntity;

                    // The component manages the set of sessions, so this invalid session should be removed soon.
                    if (attachedEntity == null || !attachedEntity.IsValid())
                        continue;

                    if (uiMap != attachedEntity.Transform.MapID)
                    {
                        ui.Close(session);
                        continue;
                    }

                    var distanceSquared = (uiPos - attachedEntity.Transform.WorldPosition).LengthSquared;
                    if (distanceSquared > MaxWindowRangeSquared)
                        ui.Close(session);
                }
            }
        }
    }
}
