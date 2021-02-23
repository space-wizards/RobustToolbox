namespace Robust.Shared.GameObjects
{
    public interface IComponentDependencyManager
    {
        /// <summary>
        /// Called when newComp has just been added into entity.
        /// </summary>
        /// <param name="entity">Entity in which newComp was added</param>
        /// <param name="newComp">Added component</param>
        public void OnComponentAdd(IEntity entity, IComponent newComp);

        /// <summary>
        /// Called when newComp has just been removed from entity.
        /// </summary>
        /// <param name="entity">Entity out of which newComp was removed</param>
        /// <param name="removedComp">Removed component</param>
        public void OnComponentRemove(IEntity entity, IComponent removedComp); }
}
