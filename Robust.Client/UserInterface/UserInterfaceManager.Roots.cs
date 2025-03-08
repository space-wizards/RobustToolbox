using System;
using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface;

//
// Contains primary UI root management logic.
//

internal sealed partial class UserInterfaceManager
{
    private readonly List<WindowRoot> _roots = new();
    private readonly Dictionary<WindowId, WindowRoot> _windowsToRoot = new();
    public IEnumerable<UIRoot> AllRoots => _roots;

    public WindowRoot CreateWindowRoot(IClydeWindow window)
    {
        if (_windowsToRoot.ContainsKey(window.Id))
        {
            throw new ArgumentException("Window already has a UI root.");
        }

        var newRoot = new WindowRoot(window)
        {
            MouseFilter = Control.MouseFilterMode.Ignore,
            HorizontalAlignment = Control.HAlignment.Stretch,
            VerticalAlignment = Control.VAlignment.Stretch
        };

        newRoot.UIScaleSet = CalculateAutoScale(newRoot);

        _roots.Add(newRoot);
        _windowsToRoot.Add(window.Id, newRoot);

        newRoot.InvalidateStyleSheet();
        newRoot.InvalidateMeasure();
        QueueMeasureUpdate(newRoot);
        QueueArrangeUpdate(newRoot);

        if (window.IsFocused)
            FocusRoot(newRoot);

        return newRoot;
    }

    public void DestroyWindowRoot(IClydeWindow window)
    {
        // Destroy window root if this window had one.
        if (!_windowsToRoot.TryGetValue(window.Id, out var root))
            return;

        if (root == _focusedRoot)
            UnfocusRoot(root);

        _windowsToRoot.Remove(window.Id);
        _roots.Remove(root);

        root.RemoveAllChildren();
    }

    public WindowRoot? GetWindowRoot(IClydeWindow window)
    {
        return !_windowsToRoot.TryGetValue(window.Id, out var root) ? null : root;
    }

    private void ClydeOnWindowFocused(WindowFocusedEventArgs eventArgs)
    {
        if (GetWindowRoot(eventArgs.Window) is not { } root)
            return;

        if (eventArgs.Focused)
        {
            // Focusing new window.
            FocusRoot(root);
        }
        else
        {
            // Unfocusing, should be the active window.
            if (root != _focusedRoot)
            {
                /*_sawmillUI.Warning(
                    "Unfocused window, but its root wasn't focused already! Window: {WindowId}",
                    eventArgs.Window.Id);*/
                return;
            }

            UnfocusRoot(root);
        }
    }

    private void FocusRoot(WindowRoot root)
    {
        DebugTools.Assert(_roots.Contains(root), "Tried to focus invalid UI root.");

        if (_focusedRoot != null)
        {
            _sawmillUI.Warning("Already had a focused UI root! Replacing...");

            UnfocusRoot(_focusedRoot);

            DebugTools.Assert(_focusedRoot == null);
        }

        _focusedRoot = root;

        // Try to restore keyboard-focused UI control from new root.
        ref var stored = ref root.StoredKeyboardFocus;
        if (stored != null)
        {
            DebugTools.Assert(
                stored.IsInsideTree,
                "Stored focused control on root was not inside UI tree anymore!");

            DebugTools.Assert(
                stored.Root == root,
                "Stored focused control on root wasn't inside root's own tree!");

            GrabKeyboardFocus(stored);
            stored = null;
        }
    }

    private void UnfocusRoot(WindowRoot root)
    {
        var controlFocused = KeyboardFocused;
        if (controlFocused != null)
        {
            if (controlFocused.Root != root)
            {
                _sawmillUI.Warning("Keyboard focused control isn't inside focused UI root!");
            }
            else
            {
                // Save focused control on window root so we can restore it when re-focusing.
                root.StoredKeyboardFocus = controlFocused;
                ReleaseKeyboardFocus(controlFocused);
            }
        }

        _focusedRoot = null;
    }
}
