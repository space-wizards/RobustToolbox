using System;
using SS14.Shared.Serialization;

namespace SS14.Shared.ViewVariables
{
    [Serializable, NetSerializable]
    public class ViewVariablesPropertySelector
    {
        public ViewVariablesPropertySelector(int index)
        {
            Index = index;
        }

        public int Index { get; set; }
    }

    [Serializable, NetSerializable]
    public class ViewVariablesEnumerableIndexSelector
    {
        public ViewVariablesEnumerableIndexSelector(int index)
        {
            Index = index;
        }

        public int Index { get; set; }
    }
}
