using System.Linq;
using Robust.Shared.Interfaces.GameObjects;

namespace Robust.Shared.Localization.Macros
{
    /// <summary>
    /// Genders for grammatical usage only.
    /// </summary>
    public enum Gender : byte
    {
        Epicene,
        Female,
        Male,
        Neuter,
    }

    public interface IGenderable
    {
        public Gender Gender => Gender.Epicene;

        /// <summary>
        /// Helper function to get the gender of an object, or Epicene if the object is not IGenderable
        /// </summary>
        public static Gender GetGenderOrEpicene(object? argument)
        {
            // FIXME The Entity special case is not really good
            if (argument is IEntity entity)
            {
                // FIXME And this is not really better.
                return Enumerable.FirstOrDefault(entity.GetAllComponents<IGenderable>())?.Gender ?? Gender.Epicene;
            }
            else
                return (argument as IGenderable)?.Gender ?? Gender.Epicene;
        }
    }

    public interface IProperNamable
    {
        public bool Proper => false;

        public static bool GetProperOrFalse(object? argument)
        {
            // FIXME The Entity special case is not really good
            if (argument is IEntity entity)
            {
                // FIXME And this is not really better.
                return entity.GetAllComponents<IProperNamable>().FirstOrDefault()?.Proper ?? false;
            }

            return (argument as IProperNamable)?.Proper ?? false;
        }
    }
}
