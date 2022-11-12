using Robust.Shared.GameObjects;

namespace Robust.Client.GameObjects
{
    public static class EntityManagerExt
    {
        public static void RaisePredictiveEvent<T>(this IEntityManager entityManager, T msg)
            where T : EntityEventArgs
        {
            ((IClientEntityManager)entityManager).RaisePredictiveEvent(msg);
        }
    }
}
