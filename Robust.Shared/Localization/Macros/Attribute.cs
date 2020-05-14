namespace Robust.Shared.Localization.Macros
{
    /// <summary>
    /// "Possible" gender for grammatical usage only.
    /// Arranged in alphabetical order so as not to offend anyone.
    /// </summary>
    public enum Gender
    {
        Epicene,
        Female,
        Male,
        Neuter,
    }

    public interface IGenderable
    {
        public Gender Gender => Gender.Neuter;

        /// <summary>
        /// Helper function to get the gender of an object, or Epicene if the object is not IGenderable
        /// </summary>
        public static Gender GetGenderOrEpicene(object argument)
        {
            return (argument as IGenderable)?.Gender ?? Gender.Epicene;
        }
    }
}
