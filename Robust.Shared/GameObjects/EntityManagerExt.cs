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
    }
}
