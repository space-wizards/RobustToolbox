using System.Collections.Immutable;
using System.Text;
using Robust.Client.Graphics;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Robust.Client.Console.Commands;

internal sealed class CompileShaderCommand : IConsoleCommand
{
    [Dependency] private readonly IShaderCompiler _shaderCompiler = null!;
    [Dependency] private readonly IResourceManager _resourceManager = null!;

    public string Command => "compile_shader";
    public string Description => "";
    public string Help => "";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var path = args[0];

        var x = _shaderCompiler.CompileToWgsl(new ResPath(path), ImmutableDictionary<string, bool>.Empty);
        if (!x.Success)
        {
            shell.WriteError("Compilation failed");
            return;
        }

        var codeText = Encoding.UTF8.GetString(x.Code);
        shell.WriteLine(codeText);
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.ContentFilePath(args[0], _resourceManager),
                "<path>");
        }

        return CompletionResult.Empty;
    }
}
