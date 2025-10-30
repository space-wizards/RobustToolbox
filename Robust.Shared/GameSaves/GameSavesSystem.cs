using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Nett;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Upload;
using Robust.Shared.Utility;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.GameSaves;

public sealed class GameSavesSystem : EntitySystem
{
    [Dependency] private readonly IResourceManager _resourceManager = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;

    public bool TrySaveGame(ResPath path)
    {
        var ev = new BeforeGameSaveEvent(path);
        RaiseLocalEvent(ref ev);

        _mapLoader.TrySaveGame(path);

        return true;
    }

    public bool TryLoadGame(ResPath path)
    {
        var ev = new BeforeGameLoadEvent(path);
        RaiseLocalEvent(ref ev);

        _mapLoader.TryLoadGame(path);

        return true;
    }
}
