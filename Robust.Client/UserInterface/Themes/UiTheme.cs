using System;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Client.UserInterface.Themes;

[Prototype("uiTheme")]
public sealed partial class UITheme : IPrototype
{
    private IResourceCache? _cache;
    private IUserInterfaceManager? _uiMan;

    //this is used for ease of access
    public const string DefaultPath = "/Textures/Interface";
    public const string DefaultName = "Default";
    public static ResPath DefaultThemePath = new ($"{DefaultPath}/{DefaultName}");

    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("path")]
    private ResPath _path;

    [DataField("colors", readOnly: true)] // This is a prototype, why is this readonly??
    public FrozenDictionary<string, Color>? Colors { get; }
    public ResPath Path => _path == default ? new ResPath(DefaultPath+"/"+ID) : _path;

    private void ValidateFilePath(IResourceManager manager)
    {
        var foundFolders = manager.ContentFindFiles(Path.ToRootedPath());
        if (!foundFolders.Any())
            throw new Exception("UITheme: "+ID+" not found in resources!");
    }

    public Texture ResolveTexture(string texturePath)
    {
        if (TryResolveTexture(texturePath, out var texture))
            return texture;

        Logger.Error($"Failed to resolve texture {texturePath}. Resorting to fallback.");
        return _cache!.GetFallback<TextureResource>();
    }

    public TextureResource? ResolveTextureOrNull(string? texturePath)
    {
        return TryResolveTexture(texturePath, out var texture) ? texture : null;
    }

    public bool TryResolveTexture(
        string? texturePath,
        [NotNullWhen(true)] out TextureResource? texture)
    {
        IoCManager.Resolve(ref _cache);

        if (texturePath == null)
        {
            texture = null;
            return false;
        }

        if (!texturePath.EndsWith(".png"))
            texturePath = $"{texturePath}.png";

        var resPath = new ResPath(texturePath);
        if (resPath.IsRelative)
        {
            if (_cache.TryGetResource( Path / resPath, out texture))
                return true;

            if (_cache.TryGetResource( DefaultThemePath / resPath, out texture))
                return true;
        }

        // using texturePath instead of resPath as absolute paths do not need to have .png appended.
        return _cache.TryGetResource(texturePath, out texture);
    }

    public Color? ResolveColor(string colorName)
    {
        if (Colors == null)
            return null;

        if (Colors.TryGetValue(colorName, out var color))
            return color;

        IoCManager.Resolve(ref _uiMan);
        if (_uiMan.DefaultTheme.Colors?.TryGetValue(colorName, out color) ?? false)
            return color;

        return null;
    }

    public Color ResolveColorOrSpecified(string colorName, Color defaultColor = default)
    {
        var color = ResolveColor(colorName) ?? defaultColor;
        return color;
    }
}
