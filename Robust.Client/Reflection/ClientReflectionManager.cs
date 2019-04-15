using Robust.Shared.Reflection;
using System.Collections.Generic;

namespace Robust.Client.Reflection
{
    /// <summary>
    ///     Implementation of <see cref="ReflectionManager"/>
    ///     that defines <c>Robust.Client.</c> and <c>Robust.Shared.</c>
    ///     as valid prefixes for <see cref="ReflectionManager.GetType(string)"/>
    /// </summary>
    public sealed class ClientReflectionManager : ReflectionManager
    {
        protected override IEnumerable<string> TypePrefixes => _typePrefixes;

        // Cache these so that we only need to allocate the array ONCE.
        private static readonly string[] _typePrefixes = new[]
        {
            "",
            "Robust.Client.",
            "Robust.Shared.",
            "Content.Shared.",
            "Content.Client."
        };
    }
}
