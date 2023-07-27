using System.Diagnostics.CodeAnalysis;
using Robust.Client.Console;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.RichText;

public sealed class CommandLinkTag : IMarkupTag
{
    [Dependency] private readonly IClientConsoleHost _clientConsoleHost = default!;

    public string Name => "cmdlink";

    /// <inheritdoc/>
    public bool TryGetControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        if (!node.Value.TryGetString(out var text)
            || !node.Attributes.TryGetValue("command", out var commandParameter)
            || !commandParameter.TryGetString(out var command))
        {
            control = null;
            return false;
        }

        var label = new Label();
        label.Text = text;

        label.MouseFilter = Control.MouseFilterMode.Stop;
        label.FontColorOverride = Color.LightBlue;
        label.DefaultCursorShape = Control.CursorShape.Hand;

        label.OnMouseEntered += _ => label.FontColorOverride = Color.Blue;
        label.OnMouseExited += _ => label.FontColorOverride = Color.LightBlue;
        label.OnKeyBindDown += args => OnKeybindDown(args, command);

        control = label;
        return true;
    }

    private void OnKeybindDown(GUIBoundKeyEventArgs args, string command)
    {
        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        _clientConsoleHost.ExecuteCommand(command);
    }
}
