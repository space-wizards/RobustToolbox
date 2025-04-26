using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Utility;

namespace Robust.Client.Localization;

internal sealed class ClientLocalizationManager : LocalizationManager, ILocalizationManagerInternal
{
    [Dependency] private readonly IReloadManager _reload = default!;

    void ILocalizationManager.Initialize() => Initialize();

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        _reload.Register(LocaleDirPath, "*.ftl");

        _reload.OnChanged += OnReload;
    }

    /// <summary>
    /// Handles Fluent hot reloading via LocalizationManager.ReloadLocalizations()
    /// </summary>
    private void OnReload(ResPath args)
    {
        if (args.Extension != "ftl")
            return;

        ReloadLocalizations();
    }
}
