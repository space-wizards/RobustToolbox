namespace Robust.Shared.Localization.Macros
{
    public class They : ITextMacro
    {
        public string Format(object argument)
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

    public class Their : ITextMacro
    {
        public string Format(object argument)
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

    public class Theirs : ITextMacro
    {
        public string Format(object argument)
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

    public class Them : ITextMacro
    {
        public string Format(object argument)
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

    public class Themself : ITextMacro
    {
        public string Format(object argument)
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
}
