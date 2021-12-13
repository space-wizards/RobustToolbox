using System;
using System.Reflection;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Shared.Utility
{
    /// <summary>
    ///     Utility class for producing debug menu representations.
    /// </summary>
    public static class PrettyPrint
    {

        /// <summary>
        ///     Get the user-facing string representation of a value.
        ///
        ///     This is intended for menus where users are required to look at
        ///     some kind of raw engine representation. It is not a substitute
        ///     for a proper UI.
        /// </summary>
        /// <param name="value">The object to represent.</param>
        /// <returns>A readable representation of the object.</returns>
        public static string? PrintUserFacing(object? value)
        {
           return PrintUserFacingWithType(value, out _);
        }

        /// <summary>
        ///     Get the user-facing string representation of a value, along with
        ///     the representation of its type.
        ///
        ///     See <see cref='PrintUserFacing(object)'/> for usage details. This
        ///     also returns a user-facing representation of the object's type in
        ///     <paramref name="typeRep"/> if it is different to that of the object.
        ///     If the object's <c>ToString()</c> implementation is the default
        ///     one, then <paramref name="typeRep"/> will be <c>""</c>.
        /// </summary>
        /// <param name="value">The object to represent.</param>
        /// <param name="typeRef">
        ///   The representation of the object's type, if distinct from the
        ///   returned value. Otherwise, <c>""</c>.
        /// </param>
        /// <returns>A readable representation of the object.</returns>
        public static string PrintUserFacingWithType(object? value, out string typeRep)
        {
            if (value == null) {
                typeRep = string.Empty;
                return "null";
            }

            string? stringRep;
            // Make best effort to guess whether or not this needs an abbreviated
            // type representation - if the type doesn't overwrite the default
            // `Object` `ToString`, then it will just print a type - so we instead
            // print the abbreviated version. Otherwise let the type print whatever
            // it wants
            if (value.GetType().GetMethod("ToString", new Type[0], new ParameterModifier[0])!.DeclaringType == typeof(Object)) {
                stringRep = TypeAbbreviation.Abbreviate(value.GetType());
                typeRep = string.Empty;
            } else if (value is EntityUid uid)
            {
                stringRep = IoCManager.Resolve<IEntityManager>().ToPrettyString(uid);
                typeRep = TypeAbbreviation.Abbreviate(value.GetType());
            }else {
                stringRep = value.ToString();
                typeRep = TypeAbbreviation.Abbreviate(value.GetType());
            }

            return stringRep!;
        }
    }
}
