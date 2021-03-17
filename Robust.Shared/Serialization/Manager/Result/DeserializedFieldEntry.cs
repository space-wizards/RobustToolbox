using System;

namespace Robust.Shared.Serialization.Manager.Result
{
    public class DeserializedFieldEntry
    {
        public DeserializedFieldEntry(bool mapped, SerializationDataDefinition.InheritanceBehaviour inheritanceBehaviour, DeserializationResult? result = null)
        {
            Mapped = mapped;
            Result = result;
            InheritanceBehaviour = inheritanceBehaviour;
        }

        public bool Mapped { get; }
        public SerializationDataDefinition.InheritanceBehaviour InheritanceBehaviour { get; }

        public DeserializationResult? Result { get; }

        public DeserializedFieldEntry PushInheritanceFrom(DeserializedFieldEntry fieldEntry)
        {
            if(Mapped)
            {
                if (InheritanceBehaviour == SerializationDataDefinition.InheritanceBehaviour.Always)
                {
                    if (Result != null)
                    {
                        return fieldEntry.Result != null
                            ? new(Mapped, InheritanceBehaviour, Result.PushInheritanceFrom(fieldEntry.Result))
                            : Copy();
                    }
                    else
                    {
                        return fieldEntry.Copy();
                    }
                }

                return Copy();
            }

            return InheritanceBehaviour == SerializationDataDefinition.InheritanceBehaviour.Never ? Copy() : fieldEntry.Copy();
        }

        public DeserializedFieldEntry Copy()
        {
            return new(Mapped, InheritanceBehaviour, Result?.Copy());
        }
    }
}
