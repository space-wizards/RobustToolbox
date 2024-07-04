using JetBrains.Annotations;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Utility;

namespace Robust.Client.Console.Commands;

[UsedImplicitly]
internal sealed class LocalizationSetCulture : LocalizedCommands
{
    [Dependency] private readonly ILocalizationManager _localization = default!;

    private const string Name = "localization_set_culture";
    private const int ArgumentCount = 1;

    public override string Command => Name;
    public override string Description => "Set DefaultCulture for the client LocalizationManager";
    public override string Help => $"{Name} <cultureName>";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != ArgumentCount)
        {
            shell.WriteError(Loc.GetString("cmd-invalid-arg-number-error"));
            return;
        }

        if (!CultureInfoHelper.TryGetCultureInfo(args[0], out var cultureInfo))
        {
            shell.WriteError(Loc.GetString("cmd-parse-failure-cultureinfo", ("args", args[0])));
            return;
        }

        _localization.SetCulture(cultureInfo);
        shell.WriteLine($"Localization changed to {cultureInfo.Name}");
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromOptions(CultureInfoHelper.CultureNames),
            _ => CompletionResult.Empty
        };
    }
}
