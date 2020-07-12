using System;
using JetBrains.Annotations;

namespace Robust.Shared.Localization.Macros
{
    /// <summary>
    /// Register a text macro. The parameter must be the name of the macro,
    /// and a an IEFT language tag might be given as a second parameter so specify the language compatible with the macro.
    /// [RegisterTextMacro("they", "en")], [RegisterTextMacro("they", "en-US")].
    ///
    /// Afterward, the macro can be use by its name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [BaseTypeRequired(typeof(ITextMacro))]
    [MeansImplicitUse]
    public sealed class RegisterTextMacroAttribute : Attribute
    {
        public readonly string MacroName;

        public readonly string? LanguageTag;

        public RegisterTextMacroAttribute(string name)
        {
            MacroName = name;
        }

        public RegisterTextMacroAttribute(string name, string languageTag)
        {
            MacroName = name;
            LanguageTag = languageTag;
        }
    }
}
