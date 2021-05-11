
namespace Robust.Shared.Enums
{
    /// <summary>
    ///     Represents a grammatical gender of an object.
    /// </summary>
    public enum Gender : byte
    {
        /// <summary>
        ///     Object has no gender and a gender probably makes no sense. Think inanimate objects. It/it.
        /// </summary>
        Neuter = 0,

        /// <summary>
        ///     Object is something you would consider as *capable of having a gender*, but *doesn't*.
        ///     Think people without (visible/unambiguous) gender. They/them.
        /// </summary>
        Epicene,

        /// <summary>
        ///     Female gender. She/her
        /// </summary>
        Female,

        /// <summary>
        ///     Male gender. He/his
        /// </summary>
        Male,
    }
}
