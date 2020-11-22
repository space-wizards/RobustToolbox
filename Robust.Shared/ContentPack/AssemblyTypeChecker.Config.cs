using System.Collections.Generic;
using System.IO;
using System.Text;
using ILVerify;
using Robust.Shared.Utility;
using YamlDotNet.Serialization;

namespace Robust.Shared.ContentPack
{
    internal sealed partial class AssemblyTypeChecker
    {
        private static SandboxConfig LoadConfig()
        {
            using var stream = typeof(AssemblyTypeChecker).Assembly
                .GetManifestResourceStream("Robust.Shared.ContentPack.Sandbox.yml")!;

            DebugTools.AssertNotNull(stream);
            return new Deserializer().Deserialize<SandboxConfig>(new StreamReader(stream, Encoding.UTF8));
        }

        private sealed class SandboxConfig
        {
            public string SystemAssemblyName = default!;
            public HashSet<VerifierError> AllowedVerifierErrors = default!;
            public List<string> WhitelistedNamespaces = default!;
            public Dictionary<string, Dictionary<string, TypeConfig>> Types = default!;
        }

        private sealed class TypeConfig
        {
            // Used for type configs where the type config doesn't exist due to a bigger-scoped All whitelisting.
            // e.g. nested types or namespace whitelist.
            public static readonly TypeConfig DefaultAll = new TypeConfig {All = true};

            public bool All;
            public InheritMode Inherit = InheritMode.Default;
            public List<string>? Methods;
            public List<string>? Fields;
            public Dictionary<string, TypeConfig>? NestedTypes;
        }

        private enum InheritMode
        {
            // Allow if All is set, block otherwise
            Default,
            Allow,
            // Block even is All is set
            Block
        }
    }
}
