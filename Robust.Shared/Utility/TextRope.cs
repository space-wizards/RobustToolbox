using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

namespace Robust.Shared.Utility;

/// <summary>
/// A binary tree data structure for efficient storage of large mutable text.
/// </summary>
/// <remarks>
/// <para>
/// <see href="https://en.wikipedia.org/wiki/Rope_(data_structure)">Read the Wikipedia article, nerd</see>
/// Also read the original paper, it's useful too.
/// </para>
/// <para>
/// Like strings, ropes are immutable and all "mutating" operations return new copies.
/// </para>
/// <para>
/// All indexing functions use <see langword="long"/> indices.
/// While individual rope leaves cannot be larger than an <see langword="int"/>, ropes with many leaves may exceed that.
/// </para>
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
    [Pure]
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
    /// Check whether the given rope is sufficiently balanced to avoid bad performance.
    /// </summary>
    [Pure]
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
    /// <remarks>
    /// If the rope is already balanced, this method does nothing.
    /// </remarks>
    [Pure]
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
    [Pure]
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
    /// Create a new rope with text spliced in at an index.
    /// </summary>
    /// <param name="rope">The rope to splice text into.</param>
    /// <param name="index">The position the inserted text should start at.</param>
    /// <param name="value">The text to insert.</param>
    /// <returns>The new rope containing the spliced data.</returns>
    [Pure]
    public static Node Insert(Node rope, long index, string value)
    {
        var (left, right) = Split(rope, index);
        return Concat(left, Concat(new Leaf(value), right));
    }

    /// <summary>
    /// Create a new rope concatenating two given ropes.
    /// </summary>
    [Pure]
    public static Node Concat(Node left, Node right)
    {
        return new Branch(left, right);
    }

    /// <summary>
    /// Create a new rope concatenating a rope and a string.
    /// </summary>
    [Pure]
    public static Node Concat(Node left, string right)
    {
        return Concat(left, new Leaf(right));
    }

    /// <summary>
    /// Create a new rope concatenating a string with a rope.
    /// </summary>
    [Pure]
    public static Node Concat(string left, Node right)
    {
        return Concat(new Leaf(left), right);
    }

    /// <summary>
    /// Return two new ropes split from the given rope at a specified index.
    /// </summary>
    [Pure]
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

    /// <summary>
    /// Create a new rope with a slice of text removed.
    /// </summary>
    /// <param name="rope">The rope to copy.</param>
    /// <param name="start">The position to start removing chars at.</param>
    /// <param name="length">How many chars to remove.</param>
    [Pure]
    public static Node Delete(Node rope, long start, long length)
    {
        var (left, _) = Split(rope, start);
        var (_, right) = Split(rope, start + length);

        return Concat(left, right);
    }

    /// <summary>
    /// Create a new rope with a given slice of text replaced with a new string.
    /// </summary>
    /// <param name="rope">The rope to copy.</param>
    /// <param name="start">The position to start removing characters at, and insert the new text at.</param>
    /// <param name="length">How many characters from the original rope to remove.</param>
    /// <param name="text">The new text to insert at the start position.</param>
    [Pure]
    public static Node ReplaceSubstring(Node rope, long start, long length, string text)
    {
        var (left, mid) = Split(rope, start);
        var (_, right) = Split(mid, length);

        return Concat(left, Concat(text, right));
    }

    /// <summary>
    /// Try to fetch a <see cref="Rune"/> at a certain position in the rune.
    /// Fails if the given position is inside a surrogate pair.
    /// </summary>
    [Pure]
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

    /// <summary>
    /// Collapse the rope into a single string instance.
    /// </summary>
    /// <exception cref="OverflowException">The given rope is too large to fit in a single string.</exception>
    [Pure]
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

    /// <summary>
    /// Collapse a substring of a rope into a single string instance.
    /// </summary>
    /// <param name="rope">The rope to collapse part of.</param>
    /// <param name="range">The range of the substring to collapse.</param>
    /// <exception cref="OverflowException">The given rope is too large to fit in a single string.</exception>
    [Pure]
    public static string CollapseSubstring(Node rope, Range range)
    {
        // TODO: Optimize
        return Collapse(rope)[range];
    }

    /// <summary>
    /// Offset a cursor position in a rope to the left, skipping over the middle of surrogate pairs.
    /// </summary>
    [Pure]
    public static long RuneShiftLeft(long index, Node rope)
    {
        index -= 1;
        if (char.IsLowSurrogate(Index(rope, index)))
            index -= 1;

        return index;
    }

    /// <summary>
    /// Offset a cursor position in a rope to the right, skipping over the middle of surrogate pairs.
    /// </summary>
    [Pure]
    public static long RuneShiftRight(long index, Node rope)
    {
        if (char.IsHighSurrogate(Index(rope, index)))
            return index + 2;

        return index + 1;
    }

    /// <summary>
    /// Returns true if the given rope is either null or empty (length 0).
    /// </summary>
    [Pure]
    public static bool IsNullOrEmpty([NotNullWhen(false)] Node? rope)
    {
        if (rope == null)
            return true;

        return CalcTotalLength(rope) == 0;
    }

    /// <summary>
    /// A nope in a rope. This is either a <see cref="Leaf"/> or a <see cref="Branch"/>.
    /// </summary>
    public abstract class Node
    {
        public abstract long Weight { get; }

        /// <summary>
        /// The depth of the deepest leaf in this node tree. A leaf has depth 0, and a branch one above 1, etc...
        /// </summary>
        public abstract short Depth { get; }
    }

    /// <summary>
    /// A leaf contains a string of text.
    /// </summary>
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
        public override short Depth => 0;
    }

    /// <summary>
    /// A branch contains other nodes to the left and right.
    /// </summary>
    [DebuggerDisplay("W: {Weight}")]
    public sealed class Branch : Node
    {
        public Node Left { get; }
        public Node? Right { get; }
        public override long Weight { get; }
        public override short Depth { get; }

        public Branch(Node left, Node? right)
        {
            Left = left;
            Right = right;
            Weight = CalcTotalLength(left);
            Depth = checked((short)(Math.Max(left.Depth, right?.Depth ?? 0) + 1));
        }
    }
}
