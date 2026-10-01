using System.Numerics;
using NUnit.Framework;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Map;

namespace Robust.UnitTesting.Client.UserInterface;

internal static class UiTestHelpers
{
    public static void ClickButton(BaseButton button)
    {
        var position = button.GlobalPixelPosition + button.Size / 2;

        var down = CreateGuiEvent(EngineKeyFunctions.UIClick, BoundKeyState.Down, position);
        var up = CreateGuiEvent(EngineKeyFunctions.UIClick, BoundKeyState.Up, position);

        button.KeyBindDown(down);
        button.KeyBindUp(up);
    }

    public static async Task ClickButton(
        RobustIntegrationTest.ClientIntegrationInstance client,
        BaseButton button)
    {
        var position = button.GlobalPixelPosition + button.Size / 2;

        await client.DoGuiEvent(button, CreateGuiEvent(EngineKeyFunctions.UIClick, BoundKeyState.Down, position));
        await client.DoGuiEvent(button, CreateGuiEvent(EngineKeyFunctions.UIClick, BoundKeyState.Up, position));
    }

    public static T FindDescendant<T>(Control control, Func<T, bool>? predicate = null)
        where T : Control
    {
        foreach (var child in control.Children)
        {
            if (child is T typed && (predicate == null || predicate(typed)))
                return typed;

            if (TryFindDescendant(child, predicate, out var found))
                return found;
        }

        throw new AssertionException($"Unable to find descendant of type {typeof(T).Name}.");
    }

    public static bool TryFindDescendant<T>(Control control, Func<T, bool>? predicate, out T found)
        where T : Control
    {
        foreach (var child in control.Children)
        {
            if (child is T typed && (predicate == null || predicate(typed)))
            {
                found = typed;
                return true;
            }

            if (TryFindDescendant(child, predicate, out found))
                return true;
        }

        found = default!;
        return false;
    }

    private static GUIBoundKeyEventArgs CreateGuiEvent(
        BoundKeyFunction function,
        BoundKeyState state,
        Vector2 position)
    {
        return new GUIBoundKeyEventArgs(
            function,
            state,
            new ScreenCoordinates(),
            true,
            Vector2.One,
            position);
    }
}
