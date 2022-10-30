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
    internal static int NextWordPosition(string str, int cursor)
    {
        return cursor + NextWordPosition(new StringEnumerateHelpers.SubstringRuneEnumerator(str, cursor));
    }

    internal static int NextWordPosition<T>(T runes) where T : IEnumerator<Rune>
    {
        if (!runes.MoveNext())
            return 0;

        var charClass = GetCharClass(runes.Current);

        var i = 0;

        IterForward(charClass);
        IterForward(CharClass.Whitespace);

        return i;

        void IterForward(CharClass cClass)
        {
            do
            {
                var rune = runes.Current;

                if (GetCharClass(rune) != cClass)
                    break;

                i += rune.Utf16SequenceLength;
            } while (runes.MoveNext());
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

        static Rune GetRuneBackwards(string str, int i)
        {
            return Rune.TryGetRuneAt(str, i, out var rune) ? rune : Rune.GetRuneAt(str, i - 1);
        }
    }

    internal static CharClass GetCharClass(Rune rune)
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

    internal enum CharClass : byte
    {
        Other,
        AlphaNumeric,
        Whitespace
    }

    internal struct CursorBlink
    {
        private const float BlinkTime = 1.3f;
        private const float BlinkStartTime = BlinkTime * -0.1f;
        private const float HalfBlinkTime = BlinkTime / 2;

        public float Opacity;
        public float Timer;

        public void Reset()
        {
            Timer = BlinkStartTime + BlinkStartTime;

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

            if (Timer > HalfBlinkTime)
                Opacity = Easings.InOutQuint((Timer - HalfBlinkTime) * (1 / HalfBlinkTime));
            else
                Opacity = 1 - Easings.InOutQuint(Timer * (1 / HalfBlinkTime));
        }
    }
}
