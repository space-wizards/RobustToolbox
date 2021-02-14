using JetBrains.Annotations;
using Robust.Shared.Localization;

namespace Robust.Client.UserInterface
{
    // TODO: Code a XAML compiler transformer to remove references to this type at compile time.
    // And just replace them with the Loc.GetString() call.
    [PublicAPI]
    public class LocExtension
    {
        public string Key { get; }

        public LocExtension(string key)
        {
            Key = key;
        }

        public object ProvideValue()
        {
            return Loc.GetString(Key);
        }
    }
}
