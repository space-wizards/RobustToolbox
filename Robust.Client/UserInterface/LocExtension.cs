using Robust.Shared.Localization;

namespace Robust.Client.UserInterface
{
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
