using System;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Robust.Client.UserInterface;

internal partial class UserInterfaceManager
{
    [ViewVariables] public float DefaultUIScale => _clyde.DefaultWindowScale.X;
    [ViewVariables] private Vector2i _resolutionAutoScaleUpper;
    [ViewVariables] private Vector2i _resolutionAutoScaleLower;
    [ViewVariables] private bool _autoScaleEnabled;
    [ViewVariables] private float _resolutionAutoScaleMinValue;

    private void _initScaling()
    {
        _clyde.OnWindowResized += WindowSizeChanged;
        _clyde.OnWindowScaleChanged += WindowContentScaleChanged;
        RegisterAutoscaleCVarListeners();
        _uiScaleChanged(_configurationManager.GetCVar(CVars.DisplayUIScale));
    }


    private void _uiScaleChanged(float newValue)
        {
            foreach (var root in _roots)
            {
                UpdateUIScale(root);
            }
        }

        private void WindowContentScaleChanged(WindowContentScaleEventArgs args)
        {
            if (_windowsToRoot.TryGetValue(args.Window.Id, out var root))
            {
                UpdateUIScale(root);
                _fontManager.ClearFontCache();
            }

        }

        private void RegisterAutoscaleCVarListeners()
        {
            _configurationManager.OnValueChanged(CVars.ResAutoScaleEnabled, i =>
            {
                _autoScaleEnabled = i;
                foreach (var root in _roots)
                {
                    root.UIScaleSet = 1;
                    _propagateUIScaleChanged(root);
                    root.InvalidateMeasure();
                }

            }, true);
            _configurationManager.OnValueChanged(CVars.ResAutoScaleLowX, i =>
            {
                _resolutionAutoScaleLower.X = i;
                foreach (var root in _roots)
                {
                    UpdateUIScale(root);
                }
            }, true);
            _configurationManager.OnValueChanged(CVars.ResAutoScaleLowY, i =>
            {
                _resolutionAutoScaleLower.Y = i;
                foreach (var root in _roots)
                {
                    UpdateUIScale(root);
                }
            }, true);
            _configurationManager.OnValueChanged(CVars.ResAutoScaleUpperX, i =>
            {
                _resolutionAutoScaleUpper.X = i;
                foreach (var root in _roots)
                {
                    UpdateUIScale(root);
                }
            }, true);
            _configurationManager.OnValueChanged(CVars.ResAutoScaleUpperY, i =>
            {
                _resolutionAutoScaleUpper.Y = i;
                foreach (var root in _roots)
                {
                    UpdateUIScale(root);
                }
            }, true);
            _configurationManager.OnValueChanged(CVars.ResAutoScaleMin, i =>
            {
                _resolutionAutoScaleMinValue = i;
                foreach (var root in _roots)
                {
                    UpdateUIScale(root);
                }
            }, true);
        }

        private float CalculateAutoScale(WindowRoot root)
        {
            //Grab the OS UIScale or the value set through CVAR debug
            var osScale = _configurationManager.GetCVar(CVars.DisplayUIScale);
            osScale = osScale == 0f ? root.Window.ContentScale.X : osScale;
            var windowSize = root.Window.RenderTarget.Size;
            //Only run autoscale if it is enabled, otherwise default to just use OS UIScale
            if (!_autoScaleEnabled && (windowSize.X <= 0 || windowSize.Y <= 0)) return osScale;
            var maxScaleRes = _resolutionAutoScaleUpper;
            var minScaleRes = _resolutionAutoScaleLower;
            var autoScaleMin = _resolutionAutoScaleMinValue;
            float scaleRatioX;
            float scaleRatioY;

            //Calculate the scale ratios and clamp it between the maximums and minimums
            scaleRatioX = Math.Clamp(((float) windowSize.X - minScaleRes.X) / (maxScaleRes.X - minScaleRes.X) * osScale, autoScaleMin, osScale);
            scaleRatioY = Math.Clamp(((float) windowSize.Y - minScaleRes.Y) / (maxScaleRes.Y - minScaleRes.Y) * osScale, autoScaleMin, osScale);
            //Take the smallest UIScale value and use it for UI scaling
            return Math.Min(scaleRatioX, scaleRatioY);
        }

        private void UpdateUIScale(WindowRoot root)
        {
            root.UIScaleSet = CalculateAutoScale(root);
            _propagateUIScaleChanged(root);
            root.InvalidateMeasure();
        }

        private static void _propagateUIScaleChanged(Control control)
        {
            control.UIScaleChanged();

            foreach (var child in control.Children)
            {
                _propagateUIScaleChanged(child);
            }
        }

        private void WindowSizeChanged(WindowResizedEventArgs windowResizedEventArgs)
        {
            if (!_windowsToRoot.TryGetValue(windowResizedEventArgs.Window.Id, out var root))
                return;
            UpdateUIScale(root);
            root.InvalidateMeasure();
        }
}
