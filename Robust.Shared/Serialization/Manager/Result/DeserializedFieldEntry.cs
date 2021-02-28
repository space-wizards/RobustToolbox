namespace Robust.Shared.Serialization.Manager.Result
{
    public class DeserializedFieldEntry
    {
        public DeserializedFieldEntry(bool mapped, DeserializationResult? result = null, bool alwaysInherit = false)
        {
            Mapped = mapped;
            Result = result;
            AlwaysInherit = alwaysInherit;
        }

        public bool Mapped { get; }
        public bool AlwaysInherit { get; }

        public DeserializationResult? Result { get; }

        public DeserializedFieldEntry PushInheritanceFrom(DeserializedFieldEntry fieldEntry)
        {
            if(Mapped)
            {
                if (AlwaysInherit)
                {
                    if(Result != null)
                    {
                        return fieldEntry.Result != null ? new (Mapped, Result.PushInheritanceFrom(fieldEntry.Result), AlwaysInherit) : Copy();
                    }
                    else
                    {
                        return fieldEntry.Copy();
                    }
                }

                return Copy();
            }
            else
            {
                return fieldEntry.Copy();
            }
        }

        public DeserializedFieldEntry Copy()
        {
            return new(Mapped, Result?.Copy(), AlwaysInherit);
        }
    }
}
