using SFML.System;
using SS14.Shared.Interfaces.GameObjects;

namespace SS14.Server.Interfaces.GameObjects
{
    public interface ITransformComponent : IComponent
    {
        Vector2f Position { get; set; }
        void TranslateTo(Vector2f toPosition);
        void TranslateByOffset(Vector2f offset);
    }
}
