using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Client.UserInterface.Themes;

[Prototype("uiTheme")]
public sealed class UITheme : IPrototype
{ //this is used for ease of access
    public const string DefaultPath = "/Textures/Interface";
    public const string DefaultName = "Default";

    [ViewVariables]
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("path")]
    private ResourcePath? _path;

    [DataField("colors", readOnly: true)]
    public Dictionary<string, Color>? Colors { get; }
    public ResourcePath Path => _path == null ? new ResourcePath(DefaultPath+"/"+ID) : _path;

    private void ValidateFilePath(IResourceCache resourceCache)
    {
        var foundFolders = resourceCache.ContentFindFiles(Path.ToRootedPath());
        if (!foundFolders.Any()) throw new Exception("UITheme: "+ID+" not found in resources!");
    }
    //helper to autoresolve dependencies
    public Texture ResolveTexture(string texturePath)
    {
        return ResolveTexture(IoCManager.Resolve<IResourceCache>(), texturePath);
    }
    public Texture ResolveTexture(IResourceCache cache, string texturePath)
    {
        return cache.TryGetResource<TextureResource>( new ResourcePath($"{Path}/{texturePath}.png"), out var texture) ? texture :
            cache.GetResource<TextureResource>($"{DefaultPath}/{DefaultName}/{texturePath}.png");
    }

    public Color? ResolveColor(string colorName)
    {
        if (Colors == null) return null;
        return Colors.TryGetValue(colorName, out var color) ? color : IoCManager.Resolve<IUserInterfaceManager>().DefaultTheme.ResolveColor(colorName);
    }

    public Color ResolveColorOrSpecified(string colorName, Color defaultColor = default)
    {
        var color = ResolveColor(colorName) ?? defaultColor;
        return color;
    }
}
