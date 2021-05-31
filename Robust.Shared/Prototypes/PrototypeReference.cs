using System;
using Robust.Shared.Serialization.Manager.Definition;

namespace Robust.Shared.Prototypes
{
    public class PrototypeReference
    {
        public PrototypeReference(ref object parent, FieldDefinition field)
        {
            Parent = new WeakReference<object>(parent);
            Field = field;
        }

        public WeakReference<object> Parent { get; }

        public FieldDefinition Field { get; }

        public void Set(IPrototype? prototype)
        {
            if (Parent.TryGetTarget(out var target))
            {
                Field.BackingField.SetValue(target, prototype);
            }
        }
    }
}
