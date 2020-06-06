using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    /// Tag type for defining the representation of rendering draw depth in
    /// terms of named constants in the content. To understand more about the
    /// point of this type, see the <see cref="ConstantsForAttribute"/>.
    /// </summary>
    public sealed class DrawDepth
    {
        /// <summary>
        /// The default draw depth. The content enum which represents draw depth
        /// should respect this value, since it is used in the engine.
        /// </summary>
        public const int Default = 0;
    }
}
