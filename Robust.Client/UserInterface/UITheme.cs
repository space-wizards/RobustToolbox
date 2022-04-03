using System;
using System.Linq;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface;
public sealed class UITheme
{
    //Set this during compile time or you will get angry exceptions :D
    public static UITheme Default = new("Default");
    public static string UIPath = "/Textures/Interface";

    internal static void ValidateDefaults()
    {
        var foundFolders = IoCManager.Resolve<IResourceCache>()
            .ContentFindFiles(new ResourcePath(UIPath).ToRootedPath());
        if (!foundFolders.Any()) throw new Exception("Default path for UI theme does not exist!");
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
