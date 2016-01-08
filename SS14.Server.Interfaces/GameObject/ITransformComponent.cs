using SFML.System;
using SS14.Shared;
using SS14.Shared.Maths;

namespace SS14.Server.Interfaces.GOC
{
    public interface ITransformComponent
    {
        Vector2f Position { get; set; }
        void TranslateTo(Vector2f toPosition);
        void TranslateByOffset(Vector2f offset);
    }
}