using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Robust.Shared.Collections;
using Robust.Shared.Console;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.Syntax;

public sealed partial class ParserContext
{
    public readonly ToolshedManager Toolshed;
    public readonly ToolshedEnvironment Environment;

    /// <summary>
    /// The parser to use when trying autocomplete variable names or to infer the type of a variable.
    /// </summary>
    /// <remarks>
    /// Unless a command uses custom parsing code, the parser context will be unaware if a command modifies a variable's
    /// type during invocation. As a result, autocompletion may be inaccurate, and invocation may cause a
    /// <see cref="VarRef{T}.BadVarTypeError"/> if the command that was parsed relied on knowing a variable's type.
    /// </remarks>
    public IVariableParser VariableParser;

    /// <summary>
    /// Arguments for the command that is currently being parsed. Useful for parsing context dependent types. E.g.,
    /// command type arguments that depend on the piped type.
    /// </summary>
    public CommandArgumentBundle Bundle;

    /// <summary>
    /// Whether or not to generate auto-completion options.
    /// </summary>
    public bool GenerateCompletions;

    /// <summary>
    /// Any auto-completion suggestions that have been generated while parsing.
    /// </summary>
    public CompletionResult? Completions;

    /// <summary>
    /// Any errors that have come up while parsing. This is generally null while <see cref="GenerateCompletions"/> is true,
    /// under the assumption that the command is purely being parsed to gather completion suggestions, not to try evaluate it.
    /// </summary>
    public IConError? Error;

    public readonly string Input;
    public int MaxIndex { get; }

    public int Index { get; private set; }

    public readonly ICommonSession? Session;

    /// <summary>
    /// Whether the parser has reached the end of the input.
    /// </summary>
    public bool OutOfInput => Index > MaxIndex;

    public ParserContext(string input, ToolshedManager toolshed, ToolshedEnvironment environment, IVariableParser parser, ICommonSession? session)
    {
        Toolshed = toolshed;
        Environment = environment;
        Input = input;
        MaxIndex = input.Length - 1;
        VariableParser = parser;
        Session = session;
    }

    public ParserContext(string input, ToolshedManager toolshed) : this(input, toolshed, toolshed.DefaultEnvironment, IVariableParser.Empty, null)
    {
    }

    public ParserContext(string input, ToolshedManager toolshed, IInvocationContext ctx) : this(input, toolshed, ctx.Environment, new InvocationCtxVarParser(ctx), ctx.Session)
    {
    }

    private ParserContext(ParserContext parserContext, int sliceSize, int? index)
    {
        Toolshed = parserContext.Toolshed;
        Environment = parserContext.Environment;
        DebugTools.Assert(sliceSize > 0);
        Input = parserContext.Input;
        Index = index ?? parserContext.Index;
        MaxIndex = Math.Min(parserContext.MaxIndex, Index + sliceSize - 1);
        VariableParser = parserContext.VariableParser;
        Session = parserContext.Session;
    }

    public bool EatMatch(char c) => EatMatch(new Rune(c));

    public bool EatMatch(Rune c)
    {
        if (PeekRune() is not { } next || next != c)
            return false;

        Index += c.Utf16SequenceLength;
        return true;
    }

    public bool EatMatch(string c)
    {
        // TODO TOOLSHED Optimize
        // Combine into one method, remove allocations.
        // I.e., this unnecessarily creates two strings.
        if (PeekWord() != c)
            return false;

        GetWord();
        return true;
    }

    /// <remarks>
    ///     This should only be used for comparisons! It'll return '\0' (NOT null) for large runes.
    /// </remarks>
    public char? PeekChar()
    {
        if (PeekRune() is not { } rune)
            return null;

        if (rune.Utf16SequenceLength > 1)
            return '\x01';
        Span<char> buffer = stackalloc char[2];
        rune.EncodeToUtf16(buffer);

        return buffer[0];
    }

    public Rune? PeekRune()
    {
        if (MaxIndex < Index)
            return null;

        return Rune.GetRuneAt(Input, Index);
    }

    public Rune? GetRune()
    {
        if (PeekRune() is not { } c)
            return null;

        Index += c.Utf16SequenceLength;
        return c;
    }

    /// <remarks>
    ///     This should only be used for comparisons! It'll return '\0' (NOT null) for large runes.
    /// </remarks>
    public char? GetChar()
    {
        if (PeekRune() is not { } c)
            return null;

        Index += c.Utf16SequenceLength;

        if (c.Utf16SequenceLength > 1)
            return '\x01';

        Span<char> buffer = stackalloc char[2];
        c.EncodeToUtf16(buffer);

        return buffer[0];
    }

    [PublicAPI]
    public void DebugPrint()
    {
        Logger.DebugS("parser", string.Join(", ", _blockStack));
        Logger.DebugS("parser", Input);
        MakeDebugPointer(Index);
        MakeDebugPointer(MaxIndex, '|');
    }

    private void MakeDebugPointer(int pointAt, char pointer = '^')
    {
        var builder = new StringBuilder();
        builder.Append(' ', pointAt);
        builder.Append(pointer);
        Logger.DebugS("parser", builder.ToString());
    }

    private string? MaybeGetWord(bool advanceIndex, Func<Rune, bool>? test)
    {
        var startingIndex = Index;
        test ??= static c => !Rune.IsWhiteSpace(c);

        var builder = new StringBuilder();

        ConsumeWhitespace();

        // Walk forward until we run into whitespace
        while (PeekRune() is { } c && test(c))
        {
            builder.Append(GetRune());
        }

        if (startingIndex == Index)
            return null;

        if (!advanceIndex)
            Index = startingIndex;

        return builder.ToString();
    }

    public string? PeekWord(Func<Rune, bool>? test = null) => MaybeGetWord(false, test);

    public string? GetWord(Func<Rune, bool>? test = null) => MaybeGetWord(true, test);

    public bool TryMatch(Regex match, int max = int.MaxValue)
    {
        ValueList<char> chars = new(8);
        // Encoding buffer.
        Span<char> encoded = stackalloc char[4];

        do
        {
            if (PeekRune() is not { } r)
                return false;
            if (max == 0)
                return false;
            max--;
            var len = r.EncodeToUtf16(encoded);
            for (var i = 0; i < len; i++)
            {
                chars.Add(encoded[i]);
            }
        } while (!match.IsMatch(chars.Span));

        return true;
    }

    public bool TryMatch(string match)
    {
        ValueList<char> chars = new(8);
        // Encoding buffer.
        Span<char> encoded = stackalloc char[4];
        var index = Index;
        var max = match.Length;
        do
        {
            if (GetRune() is not { } r)
            {
                Index = index; // Restore our position.
                return false;
            }

            if (max == 0)
            {
                Index = index;
                return false;
            }

            max--;
            var len = r.EncodeToUtf16(encoded);
            for (var i = 0; i < len; i++)
            {
                chars.Add(encoded[i]);
            }
        } while (!chars.Span.SequenceEqual(match.AsSpan()));

        return true;
    }

    public ParserRestorePoint Save()
    {
        return new ParserRestorePoint(Index, new(_blockStack), Bundle, VariableParser);
    }

    public void Restore(ParserRestorePoint point)
    {
        Index = point.Index;
        _blockStack = point.TerminatorStack;
        Bundle = point.Bundle;
        VariableParser =point.VariableParser;
    }

    public int ConsumeWhitespace()
    {
        if (NoMultilineExprs)
            return Consume(static x => Rune.IsWhiteSpace(x) && x != new Rune('\n'));
        return Consume(Rune.IsWhiteSpace);
    }

    private Stack<Rune> _blockStack = new();

    public void PushBlockTerminator(Rune term)
    {
        _blockStack.Push(term);
    }

    public void PushBlockTerminator(char term)
        => PushBlockTerminator(new Rune(term));

    public bool PeekBlockTerminator()
    {
        if (_blockStack.Count == 0)
            return false;

        return PeekRune() == _blockStack.Peek();
    }

    public bool EatBlockTerminator()
    {
        if (_blockStack.Count == 0)
            return false;

        if (!EatMatch(_blockStack.Peek()))
            return false;

        _blockStack.Pop();
        return true;
    }

    public bool CheckEndLine()
    {
        if (NoMultilineExprs)
            return EatMatch('\n');
        return false;
    }

    public int Consume(Func<Rune, bool> control)
    {
        var amount = 0;

        while (PeekRune() is { } c && control(c))
        {
            Index += c.Utf16SequenceLength;
            amount++;
        }

        return amount;
    }

    public ParserContext? SliceBlock(Rune startDelim, Rune endDelim)
    {
        var checkpoint = Save();

        ConsumeWhitespace();

        if (GetRune() != startDelim)
        {
            Restore(checkpoint);
            return null;
        }

        var blockStart = Index;

        var stack = 1;

        while (stack > 0)
        {
            var c = GetRune();
            if (c == startDelim)
                stack++;

            if (c == endDelim)
            {
                if (--stack == 0)
                    break;
            }

            if (c == null)
            {
                Restore(checkpoint);
                return null;
            }
        }

        return new ParserContext(this, Index - blockStart, blockStart);
    }

    /// <summary>
    /// Check whether a command can be invoked by the given session/user.
    /// A null session implies that the command is being run by the server.
    /// </summary>
    public bool CheckInvokable(CommandSpec cmd)
    {
        return Toolshed.CheckInvokable(cmd, Session, out Error);
    }

    /// <summary>
    /// Check whether all commands implemented by some type can be invoked by the given session/user.
    /// A null session implies that the command is being run by the server.
    /// </summary>
    public bool CheckInvokable<T>() where T : ToolshedCommand
    {
        if (!Environment.TryGetCommands<T>(out var list))
            return false;

        foreach (var x in list)
        {
            if (!CheckInvokable(x))
                return false;
        }

        return true;
    }

    public bool PeekCommandOrBlockTerminated()
    {
        if (PeekRune() is not { } c)
            return false;

        if (c == new Rune(';'))
            return true;

        if (c == new Rune('|'))
            return true;

        if (NoMultilineExprs && c == new Rune('\n'))
            return true;

        if (_blockStack.Count == 0)
            return false;

        return c == _blockStack.Peek();
    }

    /// <summary>
    /// Attempts to consume a single command terminator
    /// </summary>
    /// <param name="pipedType"></param>
    public bool EatCommandTerminator(ref Type? pipedType, out bool commandExpected)
    {
        commandExpected = false;

        // Command terminator drops piped values.
        if (EatMatch(new Rune(';')))
        {
            pipedType = null;
            return true;
        }

        // Explicit pipe operator keeps piped value, but is only valid if there is a piped value.
        if (pipedType != null && pipedType != typeof(void) && EatMatch(new Rune('|')))
        {
            commandExpected = true;
            return true;
        }

        // If multi-line commands are not enabled, we treat a newline like a ';'
        if (NoMultilineExprs && EatMatch(new Rune('\n')))
        {
            pipedType = null;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to repeatedly consume command terminators, and return true if any were consumed.
    /// </summary>
    public void EatCommandTerminators(ref Type? pipedType, out bool commandExpected)
    {
        if (!EatCommandTerminator(ref pipedType, out commandExpected))
            return;

        ConsumeWhitespace();
        while (!commandExpected && EatCommandTerminator(ref pipedType, out commandExpected))
        {
            ConsumeWhitespace();
        }
    }
}

public readonly record struct ParserRestorePoint(
    int Index,
    Stack<Rune> TerminatorStack,
    CommandArgumentBundle Bundle,
    IVariableParser VariableParser);

public record OutOfInputError : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromUnformatted("Ran out of input data when data was expected.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
