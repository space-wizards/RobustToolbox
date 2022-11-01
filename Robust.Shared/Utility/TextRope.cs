using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Robust.Shared.Utility;

/// <summary>
/// A data structure for efficient storage of large mutable text.
/// </summary>
/// <remarks>
/// <see href="https://en.wikipedia.org/wiki/Rope_(data_structure)">Read the Wikipedia article, nerd</see>
/// Also read the original paper, it's useful too.
/// </remarks>
public static class Rope
{
    internal static readonly int[] FibonacciSequence =
    {
        0, 1, 1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 233, 377, 610, 987, 1597, 2584, 4181, 6765, 10946,
        17711, 28657, 46368, 75025, 121393, 196418, 317811, 514229, 832040, 1346269, 2178309,
        3524578, 5702887, 9227465, 14930352, 24157817, 39088169, 63245986, 102334155, 165580141,
        267914296, 433494437, 701408733, 1134903170, 1836311903
    };

    /// <summary>
    /// Calculate the total text length of the rope given.
    /// </summary>
    /// <remarks>
    /// For a balanced tree, this is O(log n).
    /// </remarks>
    public static long CalcTotalLength(Node? node)
    {
        return node switch
        {
            Branch branch => branch.Weight + CalcTotalLength(branch.Right),
            Leaf leaf => leaf.Weight,
            _ => 0
        };
    }

    // TODO: Move to struct enumerator with managed stack memory.
    /// <summary>
    /// Enumerate all leaves in the rope from left to right.
    /// </summary>
    public static IEnumerable<Leaf> CollectLeaves(Node node)
    {
        var stack = new Stack<Branch>();

        var leaf = RunTillLeaf(stack, node);
        yield return leaf;

        while (stack.TryPop(out var branch))
        {
            if (branch.Right == null)
                continue;

            leaf = RunTillLeaf(stack, branch.Right);
            yield return leaf;
        }

        static Leaf RunTillLeaf(Stack<Branch> stack, Node node)
        {
            while (node is Branch branch)
            {
                stack.Push(branch);

                node = branch.Left;
            }

            return (Leaf)node;
        }
    }

    /// <summary>
    /// Enumerate all leaves in the rope from right to left.
    /// </summary>
    public static IEnumerable<Leaf> CollectLeavesReverse(Node node)
    {
        var stack = new Stack<Branch>();

        var leaf = RunTillLeaf(stack, node);
        if (leaf != null)
            yield return leaf;

        while (stack.TryPop(out var branch))
        {
            leaf = RunTillLeaf(stack, branch.Left);
            if (leaf != null)
                yield return leaf;
        }

        static Leaf? RunTillLeaf(Stack<Branch> stack, Node? node)
        {
            while (node is Branch branch)
            {
                stack.Push(branch);

                node = branch.Right;
            }

            return (Leaf?)node;
        }
    }

    // TODO: Move to struct enumerator with managed stack memory.
    /// <summary>
    /// Enumerate all text runes in the rope from left to right.
    /// </summary>
    public static IEnumerable<Rune> EnumerateRunes(Node node)
    {
        foreach (var leaf in CollectLeaves(node))
        {
            foreach (var rune in leaf.Text.EnumerateRunes())
            {
                yield return rune;
            }
        }
    }

    /// <summary>
    /// Enumerate all text runes in the rope from left to right, starting at the specified position.
    /// </summary>
    public static IEnumerable<Rune> EnumerateRunes(Node node, long startPos)
    {
        var pos = 0L;

        // Phase 1: skip over whole leaves that are before the start position.
        // TODO: Ideally we would navigate the binary tree properly instead of starting from the far left.

        // ReSharper disable once GenericEnumeratorNotDisposed
        var leaves = CollectLeaves(node).GetEnumerator();
        while (leaves.MoveNext())
        {
            var leaf = leaves.Current;
            if (pos + leaf.Weight >= startPos)
            {
                goto startLeafFound;
            }

            pos += leaf.Weight;
        }

        // Didn't find a starting leaf, must mean that startPos >= text length. Oh well?
        yield break;

        startLeafFound:

        // Phase 2: start halfway through the current leaf.
        {
            foreach (var rune in leaves.Current.Text.EnumerateRunes())
            {
                if (pos >= startPos)
                {
                    yield return rune;
                }

                pos += rune.Utf16SequenceLength;
            }
        }

        // Phase 3: just return everything from here on out.
        while (leaves.MoveNext())
        {
            var leaf = leaves.Current;
            foreach (var rune in leaf.Text.EnumerateRunes())
            {
                yield return rune;
            }
        }
    }

    /// <summary>
    /// Enumerate all the runes in the rope, from right to left.
    /// </summary>
    public static IEnumerable<Rune> EnumerateRunesReverse(Node node)
    {
        foreach (var leaf in CollectLeavesReverse(node))
        {
            var enumerator = new StringEnumerateHelpers.SubstringReverseRuneEnumerator(leaf.Text, leaf.Text.Length);
            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        }
    }

    /// <summary>
    /// Enumerate all text runes in the rope from right to left, starting at the specified position.
    /// </summary>
    public static IEnumerable<Rune> EnumerateRunesReverse(Node node, long endPos)
    {
        var pos = CalcTotalLength(node);

        // TODO: Actually start at the position instead of skipping like a worse linked list thanks.

        foreach (var rune in EnumerateRunesReverse(node))
        {
            if (pos <= endPos)
            {
                yield return rune;
            }

            pos -= rune.Utf16SequenceLength;
        }
    }

    /// <summary>
    /// Check whether the rope is sufficiently balanced to avoid bad performance.
    /// </summary>
    public static bool IsBalanced(Node node)
    {
        var depth = node.Depth;
        if (depth > FibonacciSequence.Length - 2)
            return false;

        return FibonacciSequence[depth + 2] <= node.Weight;
    }

    /// <summary>
    /// Ensure the rope is balanced to ensure decent performance on various operations.
    /// </summary>
    public static Node Rebalance(Node node)
    {
        if (IsBalanced(node))
            return node;

        var leaves = CollectLeaves(node).ToArray();
        return Merge(leaves);

        static Node Merge(ReadOnlySpan<Leaf> leaves)
        {
            if (leaves.Length == 1)
                return leaves[0];

            if (leaves.Length == 2)
                return new Branch(leaves[0], leaves[1]);

            var mid = leaves.Length / 2;
            return new Branch(Merge(leaves[..mid]), Merge(leaves[mid..]));
        }
    }

    /// <summary>
    /// Get a <see langword="char" /> at the specified index in the rope.
    /// </summary>
    /// <remarks>
    /// For a balanced tree, this is O(log n).
    /// </remarks>
    public static char Index(Node rope, long index)
    {
        switch (rope)
        {
            case Branch branch:
                if (branch.Weight > index)
                    return Index(branch.Left, index);

                if (branch.Right == null)
                    throw new IndexOutOfRangeException();

                return Index(branch.Right, index - branch.Weight);

            case Leaf leaf:
                return leaf.Text[(int)index];

            default:
                throw new ArgumentOutOfRangeException(nameof(rope));
        }
    }

    /// <summary>
    /// Splice text into a rope.
    /// </summary>
    /// <param name="rope">The rope to splice text into.</param>
    /// <param name="index">The position the inserted text should start at.</param>
    /// <param name="value">The text to insert.</param>
    /// <returns>The new rope containing the spliced data.</returns>
    public static Node Insert(Node rope, long index, string value)
    {
        var (left, right) = Split(rope, index);
        return Concat(left, Concat(new Leaf(value), right));
    }

    /// <summary>
    /// Join two ropes together.
    /// </summary>
    public static Node Concat(Node left, Node right)
    {
        return new Branch(left, right);
    }

    /// <summary>
    /// Join a rope with a string.
    /// </summary>
    public static Node Concat(Node left, string right)
    {
        return Concat(left, new Leaf(right));
    }

    /// <summary>
    /// Join a string with a rope.
    /// </summary>
    public static Node Concat(string left, Node right)
    {
        return Concat(new Leaf(left), right);
    }

    /// <summary>
    /// Split a rope into two at a certain index.
    /// </summary>
    public static (Node left, Node right) Split(Node rope, long index)
    {
        switch (rope)
        {
            case Branch branch:
            {
                if (branch.Weight > index)
                {
                    var (left, right) = Split(branch.Left, index);
                    return (
                        Rebalance(left),
                        Rebalance(new Branch(right, branch.Right))
                    );
                }

                if (branch.Weight < index)
                {
                    var (left, right) = Split(branch.Right ?? Leaf.Empty, index - branch.Weight);
                    return (
                        Rebalance(new Branch(branch.Left, left)),
                        Rebalance(right)
                    );
                }

                return (branch.Left, branch.Right ?? Leaf.Empty);
            }
            case Leaf leaf:
            {
                var left = new Leaf(leaf.Text[..(int)index]);
                var right = new Leaf(leaf.Text[(int)index..]);
                return (left, right);
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(rope));
        }
    }

    public static Node Delete(Node rope, long start, long length)
    {
        var (left, _) = Split(rope, start);
        var (_, right) = Split(rope, start + length);

        return Concat(left, right);
    }

    public static Node ReplaceSubstring(Node rope, long start, long length, string text)
    {
        var (left, mid) = Split(rope, start);
        var (_, right) = Split(mid, length);

        return Concat(left, Concat(text, right));
    }

    public static bool TryGetRuneAt(Node rope, long index, out Rune value)
    {
        var chr = Index(rope, index);
        if (!char.IsSurrogate(chr))
        {
            value = new Rune(chr);
            return true;
        }

        if (char.IsLowSurrogate(chr))
        {
            value = default;
            return false;
        }

        // TODO: throws if a high surrogate is at the very end of the rope.
        var lowChr = Index(rope, index + 1);
        if (!char.IsLowSurrogate(lowChr))
        {
            value = default;
            return false;
        }

        value = new Rune(chr, lowChr);
        return true;
    }

    public static string Collapse(Node rope)
    {
        var length = CalcTotalLength(rope);

        return string.Create(checked((int)length), rope, static (span, node) =>
        {
            foreach (var leaf in CollectLeaves(node))
            {
                var text = leaf.Text;
                text.CopyTo(span);
                span = span[text.Length..];
            }
        });
    }

    public static string CollapseSubstring(Node rope, Range range)
    {
        // TODO: Optimize
        return Collapse(rope)[range];
    }

    public static long RuneShiftLeft(long index, Node rope)
    {
        index -= 1;
        if (char.IsLowSurrogate(Index(rope, index)))
            index -= 1;

        return index;
    }

    public static long RuneShiftRight(long index, Node rope)
    {
        index += 1;
        if (char.IsLowSurrogate(Index(rope, index)))
            index += 1;

        return index;
    }

    public abstract class Node
    {
        public abstract long Weight { get; }
        public abstract byte Depth { get; }
    }

    [DebuggerDisplay("W: {Weight}, Text: {Text}")]
    public sealed class Leaf : Node
    {
        public static readonly Leaf Empty = new("");

        public string Text { get; }

        public Leaf(string text)
        {
            Text = text;
        }

        public override long Weight => Text.Length;
        public override byte Depth => 0;
    }

    [DebuggerDisplay("W: {Weight}")]
    public sealed class Branch : Node
    {
        public Node Left { get; }
        public Node? Right { get; }
        public override long Weight { get; }
        public override byte Depth { get; }

        public Branch(Node left, Node? right)
        {
            Left = left;
            Right = right;
            Weight = CalcTotalLength(left);
            Depth = checked((byte)(Math.Max(left.Depth, right?.Depth ?? 0) + 1));
        }
    }
}
