using System;

namespace Robust.Shared.Localization;

public readonly struct FText
{
    public readonly string Name;
    public readonly (string, object)[] Arguments;
    public readonly bool IsCultureInvariant;

    public FText(string name, (string, object)[]? arguments = null, bool isCultureInvariant = false)
    {
        Name = name;
        Arguments = arguments ?? Array.Empty<(string, object)>();
        IsCultureInvariant = isCultureInvariant;
    }

    public bool NoArguments => Arguments.Length == 0;

    public override string ToString()
    {
        return Name;
    }
}
