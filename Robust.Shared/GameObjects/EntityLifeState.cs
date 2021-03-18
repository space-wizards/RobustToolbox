namespace Robust.Shared.GameObjects
{
    public enum EntityLifeStage
    {
        /// <summary>
        /// The entity has just been created, and needs to be initialized.
        /// </summary>
        PreInit = 0,

        /// <summary>
        /// The entity is currently being initialized.
        /// </summary>
        Initializing,

        /// <summary>
        /// The entity has been initialized.
        /// </summary>
        Initialized,

        /// <summary>
        /// The map this entity is on has been initialized, so this entity has been as well.
        /// </summary>
        MapInitialized,

        /// <summary>
        /// The entity is currently removing all of it's components and is about to be deleted.
        /// </summary>
        Terminating,

        /// <summary>
        /// The entity has been deleted.
        /// </summary>
        Deleted,
    }
}
