using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Maths;

namespace Robust.Shared.Utility;

public sealed class PercentageSerializerUtility
{
    public static bool TryParse(string value, [NotNullWhen(true)] out float? result)
    {
        result = null;

        if (float.TryParse(value, out var @float))
        {
            if (@float is < 0 or > 1)
                return false;

            result = @float;
            return true;
        }

        if (value.Replace(" ", "")[^1] == '%' && int.TryParse(value[..^1], out var @int))
        {
            if (@int is < 0 or > 100)
                return false;

            result = (float) @int / 100;
            return true;
        }

        return false;
    }

    public static bool TryParseRange(string value, [NotNullWhen(true)] out PercentageRange? result)
    {
        result = null;
        var values = new List<float>();

        var commas = value.Split(',');

        if (commas.Length == 2)
        {
            foreach (var val in value.Split(','))
            {
                if (!TryParse(val, out var res))
                    return false;

                values.Add(res.Value);
            }

            result = new PercentageRange(values);
            return true;
        }

        values.Clear();

        var dashes = value.Split('-');

        if (dashes.Length != 2)
            return false;

        dashes[0] = dashes[0] + '%';

        foreach (var val in dashes)
        {
            if (!TryParse(val, out var res))
                return false;

            values.Add(res.Value);
        }

        result = new PercentageRange(values);
        return true;
    }
}
