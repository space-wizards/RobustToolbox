using System.Diagnostics.CodeAnalysis;

namespace Robust.Shared.Utility
{

    public static class UniqueIndexExtensions {

        [SuppressMessage("ReSharper", "RedundantAssignment")]
        public static void Clear<TKey, TValue>(ref this UniqueIndex<TKey, TValue> index) => index = default;

    }

}
