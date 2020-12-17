using System;

namespace Robust.Shared.Injections
{
    public class DirtyAttribute : Attribute
    {
        public readonly bool OnlyOnNewValue;

        public DirtyAttribute(bool onlyOnNewValue = true)
        {
            OnlyOnNewValue = onlyOnNewValue;
        }
    }
}
