using System.Collections.Generic;

namespace Robust.Shared.Interfaces.Serialization
{
    public interface IDeepClone
    {
        IDeepClone DeepClone();
    }

    public static class DeepCloneExtensions
    {
        public static IEnumerable<TValue> DeepClone<TValue>(this IEnumerable<TValue> list) where TValue : IDeepClone
        {
            var clone = new List<TValue>();
            foreach (var value in list)
            {
                clone.Add((TValue) value.DeepClone());
            }

            return clone;
        }

        public static TValue DeepClone<TValue>(this TValue value) where TValue : unmanaged
        {
            return value;
        }
    }

    /*todo dictionary deepclone
    public static Dictionary<TKey, TValue> DeepClone<TKey, TValue>(this Dictionary<TKey, TValue> dict)
        where TValue : IDeepClone where TKey : IDeepClone
    {
        var clone = new Dictionary<TKey, TValue>();
        foreach (var pair in dict)
        {
            clone.Add((TKey)pair.Key.DeepClone(), (TValue)pair.Value.DeepClone());
        }

        return clone;
    }*/
}
