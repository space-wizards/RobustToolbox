namespace Robust.Shared.GameObjects;

public static class ComponentExtensions
{
    extension<T>(T comp)
        where T : IComponent
    {
        /// <summary>
        ///     Checks if a component is "attached" and in use by the game simulation.
        ///     Unattached components are data (i.e. in prototypes).
        /// </summary>
        public bool IsUnattached()
        {
            return comp.LifeStage == ComponentLifeStage.PreAdd;
        }
    }
}
