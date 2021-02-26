namespace Robust.Shared.Serialization.Manager.Result
{
    public class DeserializedFieldEntry
    {
        public DeserializedFieldEntry(bool mapped, DeserializationResult? result = null)
        {
            Mapped = mapped;
            Result = result;
        }

        public bool Mapped { get; }

        public DeserializationResult? Result { get; }

        public DeserializedFieldEntry Copy()
        {
            return new DeserializedFieldEntry(Mapped, Result?.Copy());
        }
    }
}
