using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Utility;

namespace Robust.Client.Console.Commands;

[UsedImplicitly]
internal sealed class LocalizationSetCulture : LocalizedCommands
{
    private const string Name = "localization_set_culture";
    private const int ArgumentCount = 1;

    public override string Command => Name;
    public override string Help => $"{Name} <cultureName>";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != ArgumentCount)
        {
            shell.WriteError(Loc.GetString("cmd-invalid-arg-number-error"));
            return;
        }

        CultureInfo culture;
        try
        {
            culture = CultureInfo.GetCultureInfo(args[0], predefinedOnly: false);
        }
        catch (CultureNotFoundException)
        {
            shell.WriteError(Loc.GetString("cmd-parse-failure-cultureinfo", ("args", args[0])));
            return;
        }

        LocalizationManager.SetCulture(culture);
        shell.WriteLine($"Localization changed to {culture.Name}");
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(GetCultureNames(),
                LocalizationManager.GetString("cmd-localization_set_culture-culture-name")),
            _ => CompletionResult.Empty
        };
    }

    private static HashSet<string> GetCultureNames()
    {
        var cultureInfos = CultureInfo.GetCultures(CultureTypes.AllCultures)
            .Where(x => !string.IsNullOrEmpty(x.Name))
            .ToArray();

        var allNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        allNames.UnionWith(cultureInfos.Select(x => x.TwoLetterISOLanguageName));
        allNames.UnionWith(cultureInfos.Select(x => x.Name));

        return allNames;
    }
}
