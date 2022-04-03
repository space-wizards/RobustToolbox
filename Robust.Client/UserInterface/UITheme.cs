using System;
using System.Linq;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface;
public sealed class UITheme
{
    //Set these during your game's bootstrap! BEFORE your UI gets loaded.
    public static UITheme Default = new("Default");
    private static string _uiPath = "/Textures/Interface";
    private static bool _pathIsValid;
    public static string UIPath
    {
        get => _uiPath;
        set
        {
            _pathIsValid = _uiPath == value;
            _uiPath = value;
            ValidateDefaults(IoCManager.Resolve<IResourceCache>());
            _pathIsValid = true;
        }
    }

    internal static void ValidateDefaults(IResourceCache resourceCache)
    {
        if (_pathIsValid) return; //prevent double validation
        var foundFolders = resourceCache.ContentFindFiles(new ResourcePath(UIPath).ToRootedPath());
        if (!foundFolders.Any()) throw new Exception("Default path for UI theme does not exist!");
        _pathIsValid = true;
    }
    private string HudAssetPath { get; }
    public string Name => _name;
    private readonly string _name;

    public string ResourcePath => HudAssetPath + "/"+Name+"/";

    public UITheme(string name)
    {
        _name = name;
        HudAssetPath = UIPath;
    }


    //helper to autoresolve dependencies
    public Texture ResolveTexture(string texturePath)
    {
        return ResolveTexture(IoCManager.Resolve<IResourceCache>(), texturePath);
    }
    public Texture ResolveTexture(IResourceCache cache, string texturePath)
    {
        return cache.TryGetResource<TextureResource>( new ResourcePath(ResourcePath + texturePath+".png"), out var texture) ? texture :
            cache.GetResource<TextureResource>(HudAssetPath + "/"+"Default"+"/" + texturePath);
    }
}
