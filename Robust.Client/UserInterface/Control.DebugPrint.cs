using System.Collections.Generic;
using System.Text;

namespace Robust.Client.UserInterface;

public partial class Control
{
    /// <summary>
    /// Gets a debug-printable control path. Control paths are meant purely to ease debugging/logging,
    /// they cannot be resolved back into a control.
    /// </summary>
    public static string GetDebugPath(Control? control)
    {
        var controls = new List<Control>();

        // Navigate upwards and make a complete list of controls from leaf -> root.
        for (var c = control; c != null; c = c.Parent)
        {
            controls.Add(c);
        }

        // Go over list in reverse so we go root -> leaf.
        var sb = new StringBuilder();
        for (var i = controls.Count-1; i >= 0; i--)
        {
            var c = controls[i];
            sb.Append($"/{c}");
        }

        return sb.ToString();
    }

    public override string ToString()
    {
        return Name == null ? GetType().Name : $"{Name} ({GetType().Name})";
    }
}
