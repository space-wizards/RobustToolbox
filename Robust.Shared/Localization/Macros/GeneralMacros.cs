using Robust.Shared.GameObjects;

namespace Robust.Shared.Localization.Macros
{
    [RegisterTextMacro("name")]
    public class NameMacro: ITextMacro
    {
        public string Format(object? argument)
        {
            // TODO Make entity inherit "INameable" something?
            return (argument as IEntity)?.Name ?? argument?.ToString() ?? "<null>";
        }
    }
}
