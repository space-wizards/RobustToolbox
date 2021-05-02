using Robust.Shared.Serialization.Manager.DataDefinition;

namespace Robust.Shared.Serialization.Manager.Result
{
    public class DeserializedFieldEntry
    {
        public DeserializedFieldEntry(bool mapped, InheritanceBehaviour inheritanceBehaviour, DeserializationResult? result = null)
        {
            Mapped = mapped;
            Result = result;
            InheritanceBehaviour = inheritanceBehaviour;
        }

        public bool Mapped { get; }

        public InheritanceBehaviour InheritanceBehaviour { get; }

        public DeserializationResult? Result { get; }

        public DeserializedFieldEntry PushInheritanceFrom(DeserializedFieldEntry fieldEntry)
        {
            if (Mapped)
            {
                if (InheritanceBehaviour == InheritanceBehaviour.Always)
                {
                    if (Result != null)
                    {
                        return fieldEntry.Result != null
                            ? new DeserializedFieldEntry(Mapped, InheritanceBehaviour, Result.PushInheritanceFrom(fieldEntry.Result))
                            : Copy();
                    }
                    else
                    {
                        return fieldEntry.Copy();
                    }
                }

                return Copy();
            }

            return InheritanceBehaviour == InheritanceBehaviour.Never ? Copy() : fieldEntry.Copy();
        }

        public DeserializedFieldEntry Copy()
        {
            return new(Mapped, InheritanceBehaviour, Result?.Copy());
        }
    }
}
