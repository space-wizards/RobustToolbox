using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.ContentPack;

public sealed record ResourceManifestData(
    string[] Modules,
    string? AssemblyPrefix,
    string? DefaultWindowTitle,
    string? WindowIconSet,
    string? SplashLogo,
    bool? ShowLoadingBar,
    bool AutoConnect,
    string[]? ClientAssemblies,
    Dictionary<string, string>? ModularResources
)
{
    public static readonly ResourceManifestData Default =
        new ResourceManifestData(Array.Empty<string>(), null, null, null, null, null, true, null, null);

    public static ResourceManifestData LoadResourceManifest(IResourceManager res)
    {
        // Parses /manifest.yml for game-specific settings that cannot be exclusively set up by content code.
        if (!res.TryContentFileRead("/manifest.yml", out var stream))
            return Default;

        using (stream)
        {
            return Parse(stream);
        }
    }

    public static ResourceManifestData LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            return Default;

        using var stream = File.OpenRead(filePath);
        return Parse(stream);
    }

    private static ResourceManifestData Parse(Stream stream)
    {
        var yamlStream = new YamlStream();
        using (var streamReader = new StreamReader(stream, Encoding.UTF8))
        {
            yamlStream.Load(streamReader);
        }

        if (yamlStream.Documents.Count == 0)
            return Default;

        if (yamlStream.Documents.Count != 1 || yamlStream.Documents[0].RootNode is not YamlMappingNode mapping)
        {
            throw new InvalidOperationException("Expected a single YAML document with root mapping for manifest.yml");
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

        bool? showBar = null;
        if (mapping.TryGetNode("show_loading_bar", out var showBarNode))
            showBar = showBarNode.AsBool();

        bool autoConnect = true;
        if (mapping.TryGetNode("autoConnect", out var autoConnectNode))
            autoConnect = autoConnectNode.AsBool();

        var clientAssemblies = ReadStringArray(mapping, "clientAssemblies");

        // Use the new Dictionary reader
        var modularResources = ReadResourceMods(mapping, "resources");

        return new ResourceManifestData(
            modules,
            assemblyPrefix,
            defaultWindowTitle,
            windowIconSet,
            splashLogo,
            showBar,
            autoConnect,
            clientAssemblies,
            modularResources
        );
    }

    static string[]? ReadStringArray(YamlMappingNode mapping, string key)
    {
        if (!mapping.TryGetNode(key, out var node))
            return null;

        if (node is not YamlSequenceNode sequence)
            return null;

        var array = new string[sequence.Children.Count];
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = sequence[i].AsString();
        }

        return array;
    }
    static Dictionary<string, string>? ReadResourceMods(YamlMappingNode mapping, string key)
    {
        if (!mapping.TryGetNode(key, out var node))
            return null;

        if (node is not YamlMappingNode mapNode)
            return null;

        var dict = new Dictionary<string, string>();

        foreach (var child in mapNode)
        {
            var moduleName = child.Key.AsString();  // Just "Goobstation"
            var diskPath = child.Value.AsString();   // Just "GoobResources"

            dict[moduleName] = diskPath;
        }
        return dict;
    }
}
