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

    // TODO: Move to struct enumerator with managed stack memory.
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

    // TODO: Actually start at the position instead of skipping like a worse linked list thanks.
    public static IEnumerable<Rune> EnumerateRunes(Node node, long startPos)
    {
        var pos = 0L;
        foreach (var rune in EnumerateRunes(node))
        {
            if (pos >= startPos)
            {
                yield return rune;
            }

            pos += rune.Utf16SequenceLength;
        }
    }

    public static bool IsBalanced(Node node)
    {
        var depth = node.Depth;
        if (depth > FibonacciSequence.Length - 2)
            return false;

        return FibonacciSequence[depth + 2] <= node.Weight;
    }

    public static Node Rebalance(Node node)
    {
        if (IsBalanced(node))
            return node;

        var leaves = CollectLeaves(node).ToArray();
        return Merge(leaves);
    }

    private static Node Merge(ReadOnlySpan<Leaf> leaves)
    {
        if (leaves.Length == 1)
            return leaves[0];

        if (leaves.Length == 2)
            return new Branch(leaves[0], leaves[1]);

        var mid = leaves.Length / 2;
        return new Branch(Merge(leaves[..mid]), Merge(leaves[mid..]));
    }

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

    public static Node Insert(Node rope, long index, string value)
    {
        var (left, right) = Split(rope, index);
        return Concat(left, Concat(new Leaf(value), right));
    }

    public static Node Concat(Node left, Node right)
    {
        return new Branch(left, right);
    }

    public static Node Concat(Node left, string right)
    {
        return Concat(left, new Leaf(right));
    }

    public static Node Concat(string left, Node right)
    {
        return Concat(new Leaf(left), right);
    }

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
