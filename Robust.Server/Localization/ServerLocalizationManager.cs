using Robust.Shared.Localization;

namespace Robust.Server.Localization;

internal sealed class ServerLocalizationManager : LocalizationManager, ILocalizationManager
{
    void ILocalizationManager.Initialize() => Initialize();
}
