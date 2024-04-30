using Robust.Shared.Collections;
using Robust.Shared.Random;

namespace Robust.Shared.GameObjects
{
    public static class EntityManagerExt
    {
        public static T? GetComponentOrNull<T>(this IEntityManager entityManager, EntityUid entityUid)
            where T : IComponent
        {
            if (entityManager.TryGetComponent(entityUid, out T? component))
                return component;

            return default;
        }

        public static T? GetComponentOrNull<T>(this IEntityManager entityManager, EntityUid? entityUid)
            where T : IComponent
        {
            if (entityUid.HasValue && entityManager.TryGetComponent(entityUid.Value, out T? component))
                return component;

            return default;
        }

        /// <summary>
        /// Picks an entity at random with the supplied component.
        /// </summary>
        public static bool TryGetRandom<TComp1>(this IEntityManager entManager, IRobustRandom random, out EntityUid entity, bool includePaused = false) where TComp1 : IComponent
        {
            var entities = new ValueList<EntityUid>();

            if (includePaused)
            {
                var query = entManager.AllEntityQueryEnumerator<TComp1>();

                while (query.MoveNext(out var uid, out _))
                {
                    entities.Add(uid);
                }
            }
            else
            {
                var query = entManager.EntityQueryEnumerator<TComp1>();

                while (query.MoveNext(out var uid, out _))
                {
                    entities.Add(uid);
                }
            }

            if (entities.Count == 0)
            {
                entity = EntityUid.Invalid;
                return false;
            }

            entity = random.Pick(entities);
            return true;
        }

        /// <summary>
        /// Picks an entity at random with the supplied components.
        /// </summary>
        public static bool TryGetRandom<TComp1, TComp2>(this IEntityManager entManager, IRobustRandom random, out EntityUid entity, bool includePaused = false)
            where TComp1 : IComponent
            where TComp2 : IComponent
        {
            var entities = new ValueList<EntityUid>();

            if (includePaused)
            {
                var query = entManager.AllEntityQueryEnumerator<TComp1, TComp2>();

                while (query.MoveNext(out var uid, out _, out _))
                {
                    entities.Add(uid);
                }
            }
            else
            {
                var query = entManager.EntityQueryEnumerator<TComp1, TComp2>();

                while (query.MoveNext(out var uid, out _, out _))
                {
                    entities.Add(uid);
                }
            }

            if (entities.Count == 0)
            {
                entity = EntityUid.Invalid;
                return false;
            }

            entity = random.Pick(entities);
            return true;
        }
    }
}
