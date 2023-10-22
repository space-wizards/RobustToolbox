using System;
using System.IO;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.ContentPack;

internal sealed record ResourceManifestData(
    string[] Modules,
    string? AssemblyPrefix,
    string? DefaultWindowTitle,
    string? WindowIconSet,
    string? SplashLogo,
    bool AutoConnect,
    string[]? ClientAssemblies
)
{
    public static readonly ResourceManifestData Default =
        new ResourceManifestData(Array.Empty<string>(), null, null, null, null, true, null);

    public static ResourceManifestData LoadResourceManifest(IResourceManager res)
    {
        // Parses /manifest.yml for game-specific settings that cannot be exclusively set up by content code.
        if (!res.TryContentFileRead("/manifest.yml", out var stream))
            return ResourceManifestData.Default;

        var yamlStream = new YamlStream();
        using (stream)
        {
            using var streamReader = new StreamReader(stream, EncodingHelpers.UTF8);
            yamlStream.Load(streamReader);
        }

        if (yamlStream.Documents.Count == 0)
            return ResourceManifestData.Default;

        if (yamlStream.Documents.Count != 1 || yamlStream.Documents[0].RootNode is not YamlMappingNode mapping)
        {
            throw new InvalidOperationException(
                "Expected a single YAML document with root mapping for /manifest.yml");
        }

        var modules = ReadStringArray(mapping, "modules") ?? Array.Empty<string>();

        string? assemblyPrefix = null;
        if (mapping.TryGetNode("assemblyPrefix", out var prefixNode))
            assemblyPrefix = prefixNode.AsString();

        string? defaultWindowTitle = null;
        if (mapping.TryGetNode("defaultWindowTitle", out var winTitleNode))
            defaultWindowTitle = winTitleNode.AsString();

        string? windowIconSet = null;
        if (mapping.TryGetNode("windowIconSet", out var iconSetNode))
            windowIconSet = iconSetNode.AsString();

        string? splashLogo = null;
        if (mapping.TryGetNode("splashLogo", out var splashNode))
            splashLogo = splashNode.AsString();

        bool autoConnect = true;
        if (mapping.TryGetNode("autoConnect", out var autoConnectNode))
            autoConnect = autoConnectNode.AsBool();

        var clientAssemblies = ReadStringArray(mapping, "clientAssemblies");

        return new ResourceManifestData(
            modules,
            assemblyPrefix,
            defaultWindowTitle,
            windowIconSet,
            splashLogo,
            autoConnect,
            clientAssemblies
        );

        static string[]? ReadStringArray(YamlMappingNode mapping, string key)
        {
            if (!mapping.TryGetNode(key, out var node))
                return null;

            var sequence = (YamlSequenceNode)node;
            var array = new string[sequence.Children.Count];
            for (var i = 0; i < array.Length; i++)
            {
                array[i] = sequence[i].AsString();
            }

            return array;
        }
    }
}
