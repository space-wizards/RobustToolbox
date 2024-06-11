using System.Globalization;
using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Robust.Shared.Console.Commands;

[UsedImplicitly]
internal sealed class LocalizationSetCulture : LocalizedCommands
{
    private const string Name = "localization_set_culture";
    private const int ArgumentCount = 1;

    public override string Command => Name;
    public override string Description => "Set DefaultCulture for the client LocalizationManager";
    public override string Help => $"{Name} <cultureName>";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != ArgumentCount)
        {
            shell.WriteLine(Loc.GetString($"cmd-invalid-arg-number-error"));
            return;
        }

        var localization = IoCManager.Resolve<ILocalizationManager>();
        var culture = new CultureInfo(args[0]);

        if ((localization.DefaultCulture?.Name ?? string.Empty) == culture.Name)
        {
            shell.WriteError($"Culture {culture.Name} has already been set as default");
            return;
        }

        if (localization.HasCulture(culture))
        {
            localization.DefaultCulture = culture;
            shell.WriteLine($"Loaded culture {culture.Name} set as default");
            return;
        }

        shell.WriteLine($"Loaded culture {culture.Name} not found, loading is in progress");
        localization.LoadCulture(culture); // It's also to set culture as default

        shell.WriteLine($"Loaded culture {culture.Name} set as default");
    }
}
