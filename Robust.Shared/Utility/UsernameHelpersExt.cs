using AHelpers = Robust.Shared.AuthLib.UsernameHelpers;

namespace Robust.Shared.Utility
{
    public static class UsernameHelpersExt
    {
        public static string ToText(this AHelpers.UsernameInvalidReason reason)
        {
            return reason switch
            {
                AHelpers.UsernameInvalidReason.Valid => "Username is... valid?",
                AHelpers.UsernameInvalidReason.Empty => "Username can't be empty.",
                AHelpers.UsernameInvalidReason.TooLong => "Username is too long.",
                AHelpers.UsernameInvalidReason.InvalidCharacter =>
                "Contains invalid characters. Only use A-Z, 0-9 and underscores.",
                _ => "Unknown reason"
            };
        }
    }
}
