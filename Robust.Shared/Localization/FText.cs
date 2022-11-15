using System;

namespace Robust.Shared.Localization;

public readonly struct FText
{
    public readonly string Name;
    public readonly (string, object)[] Arguments;

    public FText(string name, params (string, object)[]? arguments)
    {
        Name = name;
        Arguments = arguments ?? Array.Empty<(string, object)>();
    }

    public bool NoArguments => Arguments.Length == 0;

    public override string ToString()
    {
        return Name;
    }
}
