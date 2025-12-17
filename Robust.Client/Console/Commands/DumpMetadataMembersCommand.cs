using System;
using System.Linq;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;

namespace Robust.Client.Console.Commands
{
#if TOOLS
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

            var members = AssemblyTypeChecker.DumpMetaMembers(type)
                .GroupBy(x => x.IsField)
                .ToDictionary(x => x.Key, x => x.Select(t => t.Value).ToList());

            if (members.TryGetValue(true, out var fields))
            {
                fields.Sort(StringComparer.Ordinal);

                foreach (var member in fields)
                {
                    System.Console.WriteLine(@$"- ""{member}""");
                }
            }

            if (members.TryGetValue(false, out var methods))
            {
                methods.Sort(StringComparer.Ordinal);

                foreach (var member in methods)
                {
                    System.Console.WriteLine(@$"- ""{member}""");
                }
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
