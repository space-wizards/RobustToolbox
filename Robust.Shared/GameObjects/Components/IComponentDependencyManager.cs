namespace Robust.Shared.GameObjects
{
    public interface IComponentDependencyManager
    {
        /// <summary>
        /// Called when newComp has just been added into entity.
        /// </summary>
        /// <param name="eUid">Entity in which newComp was added</param>
        /// <param name="newComp">Added component</param>
        public void OnComponentAdd(EntityUid eUid, IComponent newComp);

        /// <summary>
        /// Called when newComp has just been removed from entity.
        /// </summary>
        /// <param name="eUid">Entity out of which newComp was removed</param>
        /// <param name="removedComp">Removed component</param>
        public void OnComponentRemove(EntityUid eUid, IComponent removedComp); }
}
