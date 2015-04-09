using SS14.Shared.Maths;

namespace SS14.Server.Interfaces.GOC
{
    public interface ITransformComponent
    {
        Vector2 Position { get; set; }
        void TranslateTo(Vector2 toPosition);
        void TranslateByOffset(Vector2 offset);
    }
}