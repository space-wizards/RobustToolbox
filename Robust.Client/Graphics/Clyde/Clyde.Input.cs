using System.Threading;
using Robust.Client.Input;
using Robust.Shared;
using Robust.Shared.Utility;

namespace Robust.Client.Graphics.Clyde;

internal sealed partial class Clyde
{
    private bool _usQwertyKeys;

    private void InitKeys()
    {
        _cfg.OnValueChanged(CVars.DisplayUSQWERTYHotkeys, val =>
        {
            _usQwertyKeys = val;
            RaiseInputModeChanged();
        }, true);
    }

    private void RaiseInputModeChanged()
    {
        _inputManager.InputModeChanged();
    }

    public string GetKeyName(Keyboard.Key key)
    {
        DebugTools.AssertNotNull(_windowing);

        if (Keyboard.GetSpecialKeyName(key, _loc) is { } specialName)
            return specialName;

        if (_usQwertyKeys)
            return key.ToString();

        if (_windowing!.KeyGetName(key) is not { } name)
            return _loc.GetString("input-key-unknown");

        // ToTitleCase to avoid returning anything that's not capitalized. Not 100% this is needed.
        // Just putting this in, in case any funny non-English keyboards need it.
        //
        // I'd also love to be able to cache these title-cased ahead of time,
        // but then I'd have to do cache invalidation when the culture changes. Yuck.

        var curCulture = _loc.DefaultCulture ?? Thread.CurrentThread.CurrentUICulture;
        var textInfo = curCulture.TextInfo;

        return textInfo.ToTitleCase(name);
    }
}
