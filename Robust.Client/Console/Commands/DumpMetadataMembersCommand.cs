using System;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;

namespace Robust.Client.Console.Commands
{
#if DEBUG
    internal sealed class DumpMetadataMembersCommand : LocalizedCommands
    {
        public override string Command => "dmetamem";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var type = GetType(args[0]);

            if (type == null)
            {
                shell.WriteError("That type does not exist");
                return;
            }

            foreach (var sig in AssemblyTypeChecker.DumpMetaMembers(type))
            {
                System.Console.WriteLine(@$"- ""{sig}""");
                shell.WriteLine(sig);
            }
        }

        private Type? GetType(string name)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetType(name) is { } type)
                    return type;
            }

            return null;
        }
    }
#endif
}
