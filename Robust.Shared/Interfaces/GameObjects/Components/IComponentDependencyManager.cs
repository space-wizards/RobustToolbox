using Robust.Shared.GameObjects;

namespace Robust.Shared.Interfaces.GameObjects.Components
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
        /// Injects newComp into all other components of entity which are requesting newComp.
        /// </summary>
        /// <param name="entity">Entity in which newComp was added</param>
        /// <param name="newComp">Added component</param>
        public void InjectIntoEntityComponents(IEntity entity, IComponent newComp); //todo make this internal

        /// <summary>
        /// Gathers all Components present in entity and inject them into newComp.
        /// </summary>
        /// <param name="entity">Entity in which newComp was added</param>
        /// <param name="newComp">Added component</param>
        public void InjectIntoComponent(IEntity entity, IComponent newComp); //todo make this internal

        /// <summary>
        /// Called when newComp has just been removed from entity.
        /// </summary>
        /// <param name="entity">Entity out of which newComp was removed</param>
        /// <param name="removedComp">Removed component</param>
        public void OnComponentRemove(IEntity entity, IComponent removedComp);

        /// <summary>
        /// Clears all dependencies upon removedComp of components inside entity
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="removedComp"></param>
        public void ClearDependencyInEntityComponents(IEntity entity, IComponent removedComp); //todo make this internal

        /// <summary>
        /// Clears all dependencies of comp
        /// </summary>
        /// <param name="comp"></param>
        public void ClearRemovedComponentDependencies(IComponent comp); //todo make this internal
    }
}
