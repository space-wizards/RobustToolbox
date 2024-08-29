using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using XamlX.TypeSystem;

namespace Robust.Xaml
{
    /// <summary>
    /// Helpers taken from AvaloniaUI on GitHub.
    /// </summary>
    /// <remarks>
    /// - https://github.com/AvaloniaUI/Avalonia/blob/c85fa2b9977d251a31886c2534613b4730fbaeaf/src/Avalonia.Build.Tasks/XamlCompilerTaskExecutor.cs
    /// - https://github.com/AvaloniaUI/Avalonia/blob/c85fa2b9977d251a31886c2534613b4730fbaeaf/src/Avalonia.Build.Tasks/XamlCompilerTaskExecutor.Helpers.cs
    /// </remarks>
    internal partial class XamlAotCompiler
    {
        private static readonly string[] NameSuffixes = {".xaml", ".paml", ".axaml"};

        static bool CheckXamlName(IResource r) =>
            NameSuffixes.Any(suffix => r.Name.ToLowerInvariant().EndsWith(suffix));

        interface IResource : IFileSource
        {
            string Uri { get; }
            string Name { get; }
            void Remove();

        }

        interface IResourceGroup
        {
            string Name { get; }
            IEnumerable<IResource> Resources { get; }
        }

        class EmbeddedResources : IResourceGroup
        {
            private readonly AssemblyDefinition _asm;
            public string Name => "EmbeddedResource";

            public IEnumerable<IResource> Resources => _asm.MainModule.Resources.OfType<EmbeddedResource>()
                .Select(r => new WrappedResource(_asm, r)).ToList();

            public EmbeddedResources(AssemblyDefinition asm)
            {
                _asm = asm;
            }
            class WrappedResource : IResource
            {
                private readonly AssemblyDefinition _asm;
                private readonly EmbeddedResource _res;

                public WrappedResource(AssemblyDefinition asm, EmbeddedResource res)
                {
                    _asm = asm;
                    _res = res;
                }

                public string Uri => $"resm:{Name}?assembly={_asm.Name.Name}";
                public string Name => _res.Name;
                public string FilePath => Name;
                public byte[] FileContents => _res.GetResourceData();

                public void Remove() => _asm.MainModule.Resources.Remove(_res);
            }
        }
    }
}
