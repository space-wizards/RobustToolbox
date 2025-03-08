using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;

namespace Robust.Shared.Toolshed.Commands.Generic;

[ToolshedCommand]
public sealed class EmplaceCommand : ToolshedCommand
{
    private static Type[] _parsers = [typeof(EmplaceBlockOutputParser)];
    public override Type[] TypeParameterParsers => _parsers;

    [CommandImplementation, TakesPipedTypeAsGeneric]
    TOut Emplace<TOut, TIn>(
        IInvocationContext ctx,
        [PipedArgument] TIn value,
        [CommandArgument(typeof(EmplaceBlockParser))] Block block
    )
    {
        var emplaceCtx = new EmplaceContext<TIn>(ctx, EntityManager);
        emplaceCtx.Value = value;
        return (TOut) (block.Invoke(null, emplaceCtx)!);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    IEnumerable<TOut> Emplace<TOut, TIn>(
            IInvocationContext ctx,
            [PipedArgument] IEnumerable<TIn> value,
            [CommandArgument(typeof(EmplaceBlockParser))] Block block
        )
    {
        var emplaceCtx = new EmplaceContext<TIn>(ctx, EntityManager);
        foreach (var v in value)
        {
            if (ctx.HasErrors)
                yield break;

            emplaceCtx.Value = v;
            yield return (TOut) (block.Invoke(null, emplaceCtx)!);
        }
    }

    private record EmplaceContext<T> : IInvocationContext
    {
        public EmplaceContext(IInvocationContext inner, IEntityManager entMan, T? value = default)
        {
            _inner = inner;
            _entMan = entMan;
            Value = value;

            _localVars.Add("value");
            if (typeof(T) == typeof(EntityUid))
            {
                _localVars.Add("wx");
                _localVars.Add("wy");
                _localVars.Add("proto");
                _localVars.Add("name");
                _localVars.Add("desc");
                _localVars.Add("paused");
            }
            else if (typeof(T).IsAssignableTo(typeof(ICommonSession)))
            {
                _localVars.Add("ent");
                _localVars.Add("name");
                _localVars.Add("userid");
            }
        }

        public T? Value;
        private readonly IInvocationContext _inner;
        private readonly IEntityManager _entMan;
        private readonly HashSet<string> _localVars = new();

        public bool CheckInvokable(CommandSpec command, out IConError? error)
        {
            return _inner.CheckInvokable(command, out error);
        }

        public ICommonSession? Session => _inner.Session;
        public ToolshedManager Toolshed => _inner.Toolshed;
        public NetUserId? User => _inner.User;

        public ToolshedEnvironment Environment => _inner.Environment;

        public void WriteLine(string line)
        {
            _inner.WriteLine(line);
        }

        public void ReportError(IConError err)
        {
            _inner.ReportError(err);
        }

        public IEnumerable<IConError> GetErrors()
        {
            return _inner.GetErrors();
        }

        public bool HasErrors => _inner.HasErrors;

        public void ClearErrors()
        {
            _inner.ClearErrors();
        }


        public IEnumerable<string> GetVars()
        {
            foreach (var name in _localVars)
            {
                yield return name;
            }

            foreach (var inner in _inner.GetVars())
            {
                if (!_localVars.Contains(inner))
                    yield return inner;
            }
        }

        public object? ReadVar(string name)
        {
            if (name == "value")
                return Value;

            return Value switch
            {
                EntityUid uid => name switch
                {
                    "wx" => _entMan.System<SharedTransformSystem>().GetWorldPosition(uid).X,
                    "wy" => _entMan.System<SharedTransformSystem>().GetWorldPosition(uid).Y,
                    "proto" => _entMan.GetComponent<MetaDataComponent>(uid).EntityPrototype?.ID ?? "",
                    "desc" => _entMan.GetComponent<MetaDataComponent>(uid).EntityDescription,
                    "name" => _entMan.GetComponent<MetaDataComponent>(uid).EntityName,
                    "paused" => _entMan.GetComponent<MetaDataComponent>(uid).EntityPaused,
                    _ => _inner.ReadVar(name)
                },
                ICommonSession session => name switch
                {
                    "ent" => session.AttachedEntity!,
                    "name" => session.Name,
                    "userid" => session.UserId,
                    _ => _inner.ReadVar(name)
                },
                _ => _inner.ReadVar(name)
            };
        }

        public void WriteVar(string name, object? value)
        {
            if (_localVars.Contains(name))
                ReportError(new ReadonlyVariableError(name));
            else
                _inner.WriteVar(name, value);
        }

        public bool IsReadonlyVar(string name) => _localVars.Contains(name);
    }

    /// <summary>
    /// Custom block parser for the <see cref="EmplaceCommand"/> is aware of the variables defined within the
    /// <see cref="EmplaceContext{T}"/>.
    /// </summary>
    private sealed class EmplaceBlockParser : CustomTypeParser<Block>
    {
        public static bool TryParse(ParserContext ctx, [NotNullWhen(true)] out CommandRun? result)
        {
            if (ctx.Bundle.PipedType == null)
            {
                result = null;
                return false;
            }

            // If the piped type is IEnumerable<T> we want to extract the type T.
            var pipeInferredType = ctx.Bundle.PipedType;
            if (pipeInferredType.IsGenericType(typeof(IEnumerable<>)))
                pipeInferredType = pipeInferredType.GetGenericArguments()[0];

            var localParser = SetupVarParser(ctx, pipeInferredType);
            var success = Block.TryParseBlock(ctx, null, null, out result);
            ctx.VariableParser = localParser.Inner;
            return success;
        }

        private static LocalVarParser SetupVarParser(ParserContext ctx, Type input)
        {
            var localParser = new LocalVarParser(ctx.VariableParser);
            ctx.VariableParser = localParser;
            localParser.SetLocalType("value", input, true);
            if (input == typeof(EntityUid))
            {
                localParser.SetLocalType("wx", typeof(float), true);
                localParser.SetLocalType("wy", typeof(float), true);
                localParser.SetLocalType("proto", typeof(string), true);
                localParser.SetLocalType("desc", typeof(string), true);
                localParser.SetLocalType("name", typeof(string), true);
                localParser.SetLocalType("paused", typeof(bool), true);
            }
            else if (input.IsAssignableTo(typeof(ICommonSession)))
            {
                localParser.SetLocalType("ent", typeof(EntityUid), true);
                localParser.SetLocalType("name", typeof(string), true);
                localParser.SetLocalType("userid", typeof(NetUserId), true);
            }

            return localParser;
        }

        public override bool TryParse(ParserContext ctx, [NotNullWhen(true)] out Block? result)
        {
            result = null;
            if (!TryParse(ctx, out var run))
                return false;

            result = new Block(run);
            return true;
        }

        public override CompletionResult? TryAutocomplete(ParserContext ctx, CommandArgument? arg)
        {
            TryParse(ctx, out _);
            return ctx.Completions;
        }
    }

    /// <summary>
    /// This custom parser is for parsing the type returned by the block used in the an <see cref="EmplaceCommand"/>.
    /// </summary>
    private sealed class EmplaceBlockOutputParser : CustomTypeParser<Type>
    {
        public override bool ShowTypeArgSignature => false;
        public override bool TryParse(ParserContext ctx, [NotNullWhen(true)] out Type? result)
        {
            result = null;
            var save = ctx.Save();
            if (!EmplaceBlockParser.TryParse(ctx, out var block))
                return false;

            if (block.ReturnType == null)
                return false;

            ctx.Restore(save);
            result = block.ReturnType;
            return true;
        }

        public override CompletionResult? TryAutocomplete(ParserContext ctx, CommandArgument? arg)
        {
            EmplaceBlockParser.TryParse(ctx, out _);
            return ctx.Completions;
        }
    }
}
