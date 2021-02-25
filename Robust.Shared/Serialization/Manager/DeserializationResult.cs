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

        public DeserializationResult WithObject(object? obj)
        {
            return new(obj, Mapped);
        }
    }

    public struct DeserializationEntry
    {
        public readonly bool WasMapped;
        public readonly DeserializationResult? Entry;

        public DeserializationEntry(bool wasMapped)
        {
            WasMapped = wasMapped;
            Entry = null;
        }

        public DeserializationEntry(bool wasMapped, DeserializationResult? entry)
        {
            WasMapped = wasMapped;
            Entry = entry;
        }
    }
}
