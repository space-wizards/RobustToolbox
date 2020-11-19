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
            public bool All;
            public List<string>? Methods;
            public List<string>? Fields;
            public Dictionary<string, TypeConfig>? NestedTypes;
        }
    }
}
