using System;
using Robust.Client.Interfaces.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.Maths;

namespace Robust.Client.Console.Commands
{
#if DEBUG
    internal sealed class DumpMetadataMembersCommand : IConsoleCommand
    {
        public string Command => "dmetamem";
        public string Description => "Dumps a type's members in a format suitable for the sandbox configuration file.";
        public string Help => "Usage: dmetamem <type>";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            var type = Type.GetType(args[0]);

            if (type == null)
            {
                console.AddLine("That type does not exist", Color.Red);
                return false;
            }

            foreach (var sig in AssemblyTypeChecker.DumpMetaMembers(type))
            {
                System.Console.WriteLine(@$"- ""{sig}""");
                console.AddLine(sig);
            }

            return false;
        }
    }
#endif
}
