using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Shared.GameObjects
{
    public abstract class EntitySerializer
    {
        public abstract void EntityHeader();
        public abstract void EntityFooter();

        public abstract void CompHeader();
        public abstract void CompStart();
        public abstract void CompFooter();

        public abstract void DataField<T>(ref T value, string name, T defaultValue, bool alwaysWrite = false);
    }
}
