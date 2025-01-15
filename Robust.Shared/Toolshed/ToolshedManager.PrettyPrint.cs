using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed;

public sealed partial class ToolshedManager
{
    private int _maxOutput = 128;

    /// <summary>
    ///     Pretty prints a value for use in the console.
    /// </summary>
    /// <param name="value">Value to pretty print.</param>
    /// <param name="maxOutput">The maximum number of values that can be output in a list.</param>
    /// <returns>The stringified value.</returns>
    /// <remarks>
    ///     This returns markup.
    /// </remarks>
    public string PrettyPrintType(object? value, out IEnumerable? more, bool moreUsed = false, int? maxOutput = null)
    {
        maxOutput ??= _maxOutput;
        more = null;
        if (value is null)
            return "";

        if (value is IToolshedPrettyPrint p)
        {
            return p.PrettyPrint(this, out more, moreUsed, maxOutput);
        }

        if (value is string str)
        {
            if (str.Length > 32768)
            {
                return str[..32768] + "(refusing to output more!)";
            }
            return str;
        }

        if (value is FormattedMessage msg)
            return msg.ToMarkup();

        if (value is EntityUid uid)
        {
            return _entity.ToPrettyString(uid);
        }

        if (value is Type t)
        {
            return t.PrettyName();
        }

        if (value.GetType().IsAssignableTo(typeof(IDictionary)))
        {
            var dict = ((IDictionary) value).GetEnumerator();

            var kvList = new List<string>();

            while (dict.MoveNext())
            {
                kvList.Add($"({PrettyPrintType(dict.Key, out _)}, {PrettyPrintType(dict.Value, out _)}");
            }

            return $"Dictionary {{\n{string.Join(",\n", kvList)}\n}}";
        }

        if (value is IEnumerable @enum)
        {
            var list = @enum.Cast<object?>().ToList();
            if (list.Count > maxOutput.Value)
                more = list.GetRange(maxOutput.Value, list.Count - maxOutput.Value - 1);
            var res = string.Join(",\n", list.Take(maxOutput.Value).Select(x => PrettyPrintType(x, out _)));
            if (more is not null && moreUsed)
                return res + "... (output truncated, run more for further output)";
            if (more is not null)
                return res + "... (output truncated, if possible tee the value into it's own variable)";
            return res;
        }

        return value.ToString() ?? "[unrepresentable]";
    }
}
