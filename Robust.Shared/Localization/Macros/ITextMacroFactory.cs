using System;
using System.Collections.Generic;

namespace Robust.Shared.Localization.Macros
{
    public interface ITextMacroFactory
    {
        /// <summary>
        /// Get registered macros from
        /// </summary>
        /// <param name="languageTag">IEFT language tag. Might be composed of one or two subtags. for instance, "en" or "en-US".</param>
        /// <returns>A dictionnary of macros, indexed by lower-cased macro name.</returns>
        public IDictionary<string, ITextMacro> GetMacrosForLanguage(string languageTag);

        /// <summary>
        /// Register a text macro for all languages.
        /// </summary>
        /// <param name="name">Macro name</param>
        /// <param name="macroType">The type to register</param>
        public void Register(string name, Type macroType);

        /// <summary>
        /// Register a text macro.
        /// </summary>
        /// <param name="name">Macro name</param>
        /// <param name="languageTag">IEFT tag for the language the macro applies to.</param>
        /// <param name="macroType">The type to register</param>
        public void Register(string name, string languageTag, Type macroType);

        void DoAutoRegistrations();
    }
}
