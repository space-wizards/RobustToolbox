using System;
using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using Logger = Robust.Shared.Log.Logger;

namespace Robust.Shared.Localization.Macros
{
    public class TextMacroFactory : ITextMacroFactory
    {
        [Dependency] private readonly IDynamicTypeFactoryInternal _typeFactory = default!;
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;

        private struct TextMacroRegistration
        {
            public Type MacroType;
            public string MacroName;
            public string? LanguageTag;
        }

        private IList<TextMacroRegistration> Macros = new List<TextMacroRegistration>();

        public IDictionary<string, ITextMacro> GetMacrosForLanguage(string languageTag)
        {
            var languageMacros = new Dictionary<string, ITextMacro>();

            foreach (var registeredMacro in Macros)
            {
                if (IsMacroForLanguage(languageTag, registeredMacro))
                {
                    // TODO Handle duplicate macros?
                    languageMacros.Add(registeredMacro.MacroName, _typeFactory.CreateInstanceUnchecked<ITextMacro>(registeredMacro.MacroType));
                }
            }

            return languageMacros;
        }

        private bool IsMacroForLanguage(string languageTag, TextMacroRegistration macro)
        {
            int dashIndex = languageTag.IndexOf('-');
            var firstSubTag = dashIndex != -1 ? languageTag.Substring(0, dashIndex) : languageTag;

            return macro.LanguageTag == null || macro.LanguageTag == firstSubTag || macro.LanguageTag == languageTag;
        }

        public void Register(string name, Type macroType)
        {
            Register(name, null, macroType);
        }

        public void Register(string name, string? languageTag, Type macroType)
        {
            Macros.Add(new TextMacroRegistration
            {
                MacroType = macroType,
                MacroName = name,
                LanguageTag = languageTag,
            });
        }

        public void DoAutoRegistrations()
        {
            var iComponent = typeof(ITextMacro);

            foreach (var type in _reflectionManager.FindTypesWithAttribute<RegisterTextMacroAttribute>())
            {
                if (!iComponent.IsAssignableFrom(type))
                {
                    Logger.Error("Type {0} has RegisterTextMacroAttribute but does not implement ITextMacro.", type);
                    continue;
                }

                RegisterTextMacroAttribute registerAttribute = (RegisterTextMacroAttribute)type.GetCustomAttributes(typeof(RegisterTextMacroAttribute), false)[0];
                Register(registerAttribute.MacroName, registerAttribute.LanguageTag, type);
            }
        }
    }
}
