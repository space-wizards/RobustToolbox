using Robust.Shared.Serialization.Manager.Definition;

namespace Robust.Shared.Serialization.Manager.Result
{
    public class DeserializedFieldEntry
    {
        public DeserializedFieldEntry(bool mapped, InheritanceBehavior inheritanceBehavior, DeserializationResult? result = null)
        {
            Mapped = mapped;
            Result = result;
            InheritanceBehavior = inheritanceBehavior;
        }

        public bool Mapped { get; }

        public InheritanceBehavior InheritanceBehavior { get; }

        public DeserializationResult? Result { get; }

        public DeserializedFieldEntry PushInheritanceFrom(DeserializedFieldEntry fieldEntry)
        {
            if (Mapped)
            {
                if (InheritanceBehavior == InheritanceBehavior.Always)
                {
                    if (Result != null)
                    {
                        return fieldEntry.Result != null
                            ? new DeserializedFieldEntry(Mapped, InheritanceBehavior, Result.PushInheritanceFrom(fieldEntry.Result))
                            : Copy();
                    }
                    else
                    {
                        return fieldEntry.Copy();
                    }
                }

                return Copy();
            }

            return InheritanceBehavior == InheritanceBehavior.Never ? Copy() : fieldEntry.Copy();
        }

        public DeserializedFieldEntry Copy()
        {
            return new(Mapped, InheritanceBehavior, Result?.Copy());
        }
    }
}
