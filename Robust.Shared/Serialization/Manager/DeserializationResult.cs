namespace Robust.Shared.Serialization.Manager
{
    public class DeserializationResult
    {
        public readonly object? Object;
        public readonly (bool, DeserializationResult?)[]? Mapped;

        public DeserializationResult(object? o) : this(o, null) {}

        public DeserializationResult(object? o, (bool, DeserializationResult?)[]? mapped)
        {
            Object = o;
            Mapped = mapped;
        }
    }
}
