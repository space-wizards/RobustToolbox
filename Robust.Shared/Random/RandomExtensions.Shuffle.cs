using System;
using System.Collections.Generic;
using Robust.Shared.Collections;

namespace Robust.Shared.Random;

public static partial class RandomExtensions
{
    extension<T>(T random)
        where T : IRobustRandom
    {
        /// <summary> Randomly switches positions in collection. </summary>
        public void Shuffle<TItem>(IList<TItem> list)
        {
            if (list is TItem[] arr)
            {
                // Done to avoid significant performance dip from Moq workaround in RandomExtensions.cs,
                // doubt it matters much.
                // https://github.com/space-wizards/RobustToolbox/issues/6329
                random.Shuffle(arr);
                return;
            }

            var n = list.Count;
            while (n > 1)
            {
                n -= 1;
                var k = random.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }

        /// <summary> Randomly switches positions in collection. </summary>
        public void Shuffle<TItem>(Span<TItem> list)
        {
            var n = list.Length;
            while (n > 1)
            {
                n -= 1;
                var k = random.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }

        /// <summary> Randomly switches positions in collection. </summary>
        public void Shuffle<TItem>(ValueList<TItem> list)
        {
            random.Shuffle(list.Span);
        }
    }
}
