using Robust.Shared.Toolshed;

namespace Robust.Shared.Scripting;

[ToolshedCommand]
public sealed class ILCommand : ToolshedCommand
{
    // Disabled due to relying on external dependency ILReader.
    /*
    [CommandImplementation("dumpil")]
    public void DumpIL(
        IInvocationContext ctx,
        [PipedArgument] MethodInfo info
    )
    {
        var reader = GetReader(info);

        foreach (var instruction in reader)
        {
            if (instruction is null)
                break;

            ctx.WriteLine(instruction.ToString()!);
        }
    }

    private IILReader GetReader(MethodBase method)
    {
        IILReaderConfiguration cfg = ILReader.Configuration.Resolve(method);
        return cfg.GetReader(method);
    }
    */
}

