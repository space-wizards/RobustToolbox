namespace Robust.Shared.GameObjects
{
    public static class EntityManagerExt
    {
        public static T? GetComponentOrNull<T>(this IEntityManager entityManager, EntityUid entityUid)
            where T : class, IComponent
        {
            if (entityManager.TryGetComponent(entityUid, out T? component))
                return component;

            return null;
        }
    }
}
