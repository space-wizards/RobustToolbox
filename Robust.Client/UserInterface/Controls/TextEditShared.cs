using System.Collections.Generic;
using System.Text;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.Controls;

/// <summary>
/// Shared logic between <see cref="TextEdit"/> and <see cref="LineEdit"/>
/// </summary>
internal static class TextEditShared
{
    // Approach for NextWordPosition and PrevWordPosition taken from Avalonia.

    //
    // Functions for calculating next positions when doing word-bound cursor movement (ctrl+left/right).
    //

    internal static int EndWordPosition(string str, int cursor)
    {
        return cursor + EndWordPosition(new StringEnumerateHelpers.SubstringRuneEnumerator(str, cursor));
    }

    internal static int EndWordPosition<T>(T runes) where T : IEnumerator<Rune>
    {
        if (!runes.MoveNext())
            return 0;

        var i = 0;
        if (!IterForward(CharClass.Whitespace))
            return i;

        var charClass = GetCharClass(runes.Current);
        IterForward(charClass);

        return i;

        bool IterForward(CharClass cClass)
        {
            var hasNext = true;

            do
            {
                var rune = runes.Current;

                if (GetCharClass(rune) != cClass)
                    break;

                i += rune.Utf16SequenceLength;

                hasNext = runes.MoveNext();
            } while (hasNext);

            return hasNext;
        }
    }

    internal static int PrevWordPosition(string str, int cursor)
    {
        return cursor + PrevWordPosition(new StringEnumerateHelpers.SubstringReverseRuneEnumerator(str, cursor));
    }

    internal static int PrevWordPosition<T>(T runes) where T : IEnumerator<Rune>
    {
        if (!runes.MoveNext())
            return 0;

        var startRune = runes.Current;
        var charClass = GetCharClass(startRune);

        var i = 0;
        var keepGoing = IterBackward();

        if (keepGoing && charClass == CharClass.Whitespace)
        {
            charClass = GetCharClass(runes.Current);

            IterBackward();
        }

        return i;

        bool IterBackward()
        {
            do
            {
                var rune = runes.Current;

                if (GetCharClass(rune) != charClass)
                    return true;

                i -= rune.Utf16SequenceLength;
            } while (runes.MoveNext());

            return false;
        }
    }

    private static CharClass GetCharClass(Rune rune)
    {
        if (Rune.IsWhiteSpace(rune))
        {
            return CharClass.Whitespace;
        }

        if (Rune.IsLetterOrDigit(rune))
        {
            return CharClass.AlphaNumeric;
        }

        return CharClass.Other;
    }

    private enum CharClass : byte
    {
        Other,
        AlphaNumeric,
        Whitespace
    }

    /// <summary>
    /// Helper type for the cursor blink animation.
    /// </summary>
    internal struct CursorBlink
    {
        /// <summary>
        /// The total length of the animation.
        /// </summary>
        private const float BlinkTime = 1.3f;

        // Because of the animation curves used, there is a plateau on either end of the animation.
        // 0 or t/2 in the animation, and you are exactly in the middle of this plateau.
        // Now, when we reset the blink (i.e. when the user presses a button),
        // we want this plateau to stay for a bit longer. So we offset it by this start time in that case.
        private const float BlinkStartTime = BlinkTime * -0.2f;
        private const float HalfBlinkTime = BlinkTime / 2;

        public float Opacity;
        public float Timer;

        public void Reset()
        {
            Timer = BlinkTime + BlinkStartTime;
            UpdateOpacity();
        }

        public void FrameUpdate(FrameEventArgs args)
        {
            Timer += args.DeltaSeconds;
            UpdateOpacity();
        }

        private void UpdateOpacity()
        {
            if (Timer >= BlinkTime)
                Timer %= BlinkTime;

            // Manually implement the animation function with easings. The math isn't thaaaaaaaat bad right?

            if (Timer < HalfBlinkTime)
            {
                // First half: cursor is dimming.
                Opacity = 1 - Easings.InOutQuint(Timer * (1 / HalfBlinkTime));
            }
            else
            {
                // Second half: cursor is brightening again.
                Opacity = Easings.InOutQuint((Timer - HalfBlinkTime) * (1 / HalfBlinkTime));
            }
        }
    }
}
