using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Shared.Utility
{
    public static class UsernameHelpers
    {
        private const int NameLengthMax = 32;
        private static readonly string[] InvalidStrings = new string[]
        {
            " ", // That's a space.
            "\"",
            "'",
            "DrCelt",
        };

        /// <summary>
        ///     Checks whether a user name is valid.
        ///     If this is false, feel free to kick the person requesting it. Loudly.
        /// </summary>
        /// <param name="name">The name to check.</param>
        /// <returns>True if the name is acceptable, false otherwise.</returns>
        public static bool IsNameValid(string name)
        {
            // TODO: This length check is crap and doesn't work correctly.
            // This checks the UTF-16 (LE, because Microsoft neglects the existance of endianness reliably)
            //   encoding of the string, which depending on the code points is too much.
            // Then again even code points aren't exactly reliable. Graphemes and ensuring people don't
            // go͡ ins̶a̷ne͢ w̵̩̩̤͎̳̖̥̿̉̾̊͑ͩͫ͊́͝i͇̱̘̥͖̬͇͂̈͆̒ͥ̂̾͝ͅṱ̣͎̾́̋̈́ͮḫ̟͔̪̤ͪ̇ͮ̀ Z̶͖̣̗͎̰̜͐̿ͮ͛́͒̄̓̓̐̎̃̂̿̍̚͝Aͨͮ̏̎ͨͤ̎̃͌̚͏̻̮̥͕̱͖̜̗̭͎̹͚̼͓͘ͅĻ̷̨̳͈̠͆͋̍ͧ͟Ģ̣̰̰̱̻ͪͤ̐͊̈́͑̀̂͊̆ͧ̿̚̚͜Ǫ̙͈̼͖̙̳͇͇͈̠͙̜̳̝ͨ̅̓ͮ̒ͮͥ̇́͘͜͞ might be the safest idea.
            // PS. Pretty sure Microsoft calls "grapheme clusters" "text elements"
            // This is the company that can't distinguish Unicode and UTF-16 (LE!) to save its life.
            // That's like comparing apples with the oranges in New Zealand.
            // And are then too stupid to recognize the complete worthlessness of char.
            // Seriously they should absolutely be putting 100% god damn effort into marking char as obsolete. EVERYWHERE.
            // Not surprising, huh.
            if (name.Length > 0 && name.Length >= NameLengthMax)
            {
                return false;
            }

            // TODO: Oh yeah, should probably cut out like, control characters or something.
            // I think.
            // I wanted too but gave up trying to wrangle the turd that is C#'s Unicode handling.
            // Good look, future coder.
            // There's always the option of Rust FFI.
            // I like that idea.
            // Flashbacks to libvg intensify.
            foreach (var item in InvalidStrings)
            {
                if (name.IndexOf(item) != -1)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
