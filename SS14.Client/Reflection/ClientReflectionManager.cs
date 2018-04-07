using SS14.Shared.IoC;
using SS14.Shared.Reflection;
using System.Collections.Generic;

namespace SS14.Client.Reflection
{
    /// <summary>
    ///     Implementation of <see cref="ReflectionManager"/>
    ///     that defines <c>SS14.Client.</c> and <c>SS14.Shared.</c>
    ///     as valid prefixes for <see cref="ReflectionManager.GetType(string)"/>
    /// </summary>
    public sealed class ClientReflectionManager : ReflectionManager
    {
        protected override IEnumerable<string> TypePrefixes => _typePrefixes;

        // Cache these so that we only need to allocate the array ONCE.
        private static readonly string[] _typePrefixes = new[]
        {
            "",
            "SS14.Client.",
            "SS14.Shared.",
            "Content.Shared.",
            "Content.Client."
        };
    }
}
