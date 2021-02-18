namespace Robust.Shared.Localization.Macros
{
    public interface ITextMacro
    {
        public string Format(object? argument);

        public string CapitalizedFormat(object? arg)
        {
            string result = Format(arg);
            return char.ToUpper(result[0]) + result.Substring(1);
        }
    }
}
