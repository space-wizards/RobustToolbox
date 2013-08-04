using SS13_Shared;

namespace ServerInterfaces.GOC
{
    public interface ITransformComponent
    {
        Vector2 Position { get; set; }
        void TranslateTo(Vector2 toPosition);
        void TranslateByOffset(Vector2 offset);
    }
}