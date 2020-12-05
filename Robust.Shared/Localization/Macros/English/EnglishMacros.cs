namespace Robust.Shared.Localization.Macros.English
{
    [RegisterTextMacro("they", "en")]
    public class They : ITextMacro
    {
        public string Format(object? argument)
        {
            return IGenderable.GetGenderOrEpicene(argument) switch
            {
                Gender.Female => "she",
                Gender.Male => "he",
                Gender.Neuter => "it",
                _ => "they",
            };
        }
    }

    [RegisterTextMacro("their", "en")]
    public class Their : ITextMacro
    {
        public string Format(object? argument)
        {
            return IGenderable.GetGenderOrEpicene(argument) switch
            {
                Gender.Female => "her",
                Gender.Male => "his",
                Gender.Neuter => "its",
                _ => "their",
            };
        }
    }

    [RegisterTextMacro("theirs", "en")]
    public class Theirs : ITextMacro
    {
        public string Format(object? argument)
        {
            return IGenderable.GetGenderOrEpicene(argument) switch
            {
                Gender.Female => "hers",
                Gender.Male => "his",
                Gender.Neuter => "its",
                _ => "theirs",
            };
        }
    }

    [RegisterTextMacro("them", "en")]
    public class Them : ITextMacro
    {
        public string Format(object? argument)
        {
            return IGenderable.GetGenderOrEpicene(argument) switch
            {
                Gender.Female => "her",
                Gender.Male => "him",
                Gender.Neuter => "it",
                _ => "them",
            };
        }
    }

    [RegisterTextMacro("themself", "en")]
    public class Themself : ITextMacro
    {
        public string Format(object? argument)
        {
            return IGenderable.GetGenderOrEpicene(argument) switch
            {
                Gender.Female => "herself",
                Gender.Male => "himself",
                Gender.Neuter => "itself",
                _ => "themself",
            };
        }
    }

    [RegisterTextMacro("theyre", "en")]
    public class Theyre : ITextMacro
    {
        public string Format(object? argument)
        {
            return IGenderable.GetGenderOrEpicene(argument) switch
            {
                Gender.Female => "she's",
                Gender.Male => "he's",
                Gender.Neuter => "it's",
                _ => "they're",
            };
        }
    }

    [RegisterTextMacro("theName", "en")]
    public class TheName : ITextMacro
    {
        private readonly NameMacro _nameMacro = new();

        public string Format(object? argument)
        {
            var name = _nameMacro.Format(argument);
            return IProperNamable.GetProperOrFalse(argument)
                ? name
                : "the " + name;
        }
    }

    [RegisterTextMacro("are", "en")]
    public class ToBe : ITextMacro
    {
        public string Format(object? argument)
        {
            return IGenderable.GetGenderOrEpicene(argument) switch
            {
                Gender.Female => "is",
                Gender.Male => "is",
                Gender.Neuter => "is",
                _ => "are",
            };
        }
    }

    [RegisterTextMacro("have", "en")]
    public class Have : ITextMacro
    {
        public string Format(object? argument)
        {
            return IGenderable.GetGenderOrEpicene(argument) switch
            {
                Gender.Female => "has",
                Gender.Male => "has",
                Gender.Neuter => "has",
                _ => "have",
            };
        }
    }
}
