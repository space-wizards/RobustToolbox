using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ILVerify;
using Pidgin;
using Robust.Shared.Log;
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
            var cfg = new Deserializer().Deserialize<SandboxConfig>(new StreamReader(stream, Encoding.UTF8));
            foreach (var typeCfg in cfg.Types.Values.SelectMany(p => p.Values))
            {
                ParseTypeConfig(typeCfg);
            }

            return cfg;
        }

        private static void ParseTypeConfig(TypeConfig cfg)
        {
            if (cfg.Methods != null)
            {
                var list = new List<WhitelistMethodDefine>();
                foreach (var m in cfg.Methods)
                {
                    try
                    {
                        list.Add(MethodParser.ParseOrThrow(m));
                    }
                    catch (ParseException e)
                    {
                        Logger.ErrorS("res.typecheck", $"Parse exception for '{m}': {e}");
                    }
                }

                cfg.MethodsParsed = list.ToArray();
            }
            else
            {
                cfg.MethodsParsed = Array.Empty<WhitelistMethodDefine>();
            }

            if (cfg.Fields != null)
            {
                var list = new List<WhitelistFieldDefine>();
                foreach (var f in cfg.Fields)
                {
                    try
                    {
                        list.Add(FieldParser.ParseOrThrow(f));
                    }
                    catch (ParseException e)
                    {
                        Logger.ErrorS("res.typecheck", $"Parse exception for '{f}': {e}");
                    }
                }

                cfg.FieldsParsed = list.ToArray();
            }
            else
            {
                cfg.FieldsParsed = Array.Empty<WhitelistFieldDefine>();
            }

            if (cfg.NestedTypes != null)
            {
                foreach (var nested in cfg.NestedTypes.Values)
                {
                    ParseTypeConfig(nested);
                }
            }
        }

        private sealed class SandboxConfig
        {
            public string SystemAssemblyName = default!;
            public HashSet<VerifierError> AllowedVerifierErrors = default!;
            public List<string> WhitelistedNamespaces = default!;
            public Dictionary<string, Dictionary<string, TypeConfig>> Types = default!;
        }

#pragma warning disable 649
        private sealed class TypeConfig
        {
            // Used for type configs where the type config doesn't exist due to a bigger-scoped All whitelisting.
            // e.g. nested types or namespace whitelist.
            public static readonly TypeConfig DefaultAll = new TypeConfig {All = true};

            public bool All;
            public InheritMode Inherit = InheritMode.Default;
            public string[]? Methods;
            [NonSerialized] public WhitelistMethodDefine[] MethodsParsed = default!;
            public string[]? Fields;
            [NonSerialized] public WhitelistFieldDefine[] FieldsParsed = default!;
            public Dictionary<string, TypeConfig>? NestedTypes;
        }
#pragma warning restore 649

        private enum InheritMode : byte
        {
            // Allow if All is set, block otherwise
            Default,
            Allow,

            // Block even is All is set
            Block
        }
    }
}
