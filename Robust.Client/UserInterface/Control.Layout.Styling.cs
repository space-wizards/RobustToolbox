using System;
using System.Runtime.CompilerServices;

namespace Robust.Client.UserInterface;

public partial class Control
{
    private LayoutStyleProperties _layoutStyleOverride;
    private LayoutStyleProperties _layoutStyleSheet;

    private void UpdateLayoutStyleProperties()
    {
        var propertiesSet = LayoutStyleProperties.None;

        // Assumed most controls will have little or no style properties,
        // so iterating once is less expensive overall then checking 10+ properties.
        // C# switch statements are compiled efficiently anyways.
        foreach (var (key, value) in _styleProperties)
        {
            switch (key)
            {
                case nameof(SizeFlagsStretchRatio):
                    UpdateField(ref _sizeFlagsStretchRatio, value, LayoutStyleProperties.StretchRatio);
                    break;
                case nameof(MinWidth):
                    UpdateField(ref _minWidth, value, LayoutStyleProperties.MinWidth);
                    break;
                case nameof(MinHeight):
                    UpdateField(ref _minHeight, value, LayoutStyleProperties.MinHeight);
                    break;
                case nameof(SetWidth):
                    UpdateField(ref _setWidth, value, LayoutStyleProperties.SetWidth);
                    break;
                case nameof(SetHeight):
                    UpdateField(ref _setHeight, value, LayoutStyleProperties.SetHeight);
                    break;
                case nameof(MaxWidth):
                    UpdateField(ref _maxWidth, value, LayoutStyleProperties.MaxWidth);
                    break;
                case nameof(MaxHeight):
                    UpdateField(ref _maxHeight, value, LayoutStyleProperties.MaxHeight);
                    break;
                case nameof(HorizontalExpand):
                    UpdateField(ref _horizontalExpand, value, LayoutStyleProperties.HorizontalExpand);
                    break;
                case nameof(VerticalExpand):
                    UpdateField(ref _verticalExpand, value, LayoutStyleProperties.VerticalExpand);
                    break;
                case nameof(HorizontalAlignment):
                    UpdateField(ref _horizontalAlignment, value, LayoutStyleProperties.HorizontalAlignment);
                    break;
                case nameof(VerticalAlignment):
                    UpdateField(ref _verticalAlignment, value, LayoutStyleProperties.VerticalAlignment);
                    break;
                case nameof(Margin):
                    UpdateField(ref _margin, value, LayoutStyleProperties.Margin);
                    break;
            }
        }

        // Reset cleared properties back to defaults.
        var toClear = _layoutStyleSheet & ~propertiesSet;
        if (toClear != 0)
        {
            ClearField(ref _sizeFlagsStretchRatio, DefaultStretchRatio, LayoutStyleProperties.StretchRatio);
            ClearField(ref _minWidth, 0, LayoutStyleProperties.MinWidth);
            ClearField(ref _minHeight, 0, LayoutStyleProperties.MinHeight);
            ClearField(ref _setWidth, DefaultSetSize, LayoutStyleProperties.SetWidth);
            ClearField(ref _setHeight, DefaultSetSize, LayoutStyleProperties.SetHeight);
            ClearField(ref _maxWidth, DefaultMaxSize, LayoutStyleProperties.MaxWidth);
            ClearField(ref _maxHeight, DefaultMaxSize, LayoutStyleProperties.MaxHeight);
            ClearField(ref _horizontalExpand, false, LayoutStyleProperties.HorizontalExpand);
            ClearField(ref _verticalExpand, false, LayoutStyleProperties.VerticalExpand);
            ClearField(ref _horizontalAlignment, DefaultHAlignment, LayoutStyleProperties.HorizontalAlignment);
            ClearField(ref _verticalAlignment, DefaultVAlignment, LayoutStyleProperties.VerticalAlignment);
            ClearField(ref _margin, default, LayoutStyleProperties.Margin);
        }

        _layoutStyleSheet = propertiesSet;

        return;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void UpdateField<T>(ref T field, object value, LayoutStyleProperties flag)
        {
            if ((_layoutStyleOverride & flag) != 0)
                return;

            // TODO: Probably need better error handling...
            if (value is not T valueCast)
                return;

            field = valueCast;
            propertiesSet |= flag;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ClearField<T>(ref T field, T defaultValue, LayoutStyleProperties flag)
        {
            if ((toClear & flag) == 0)
                return;

            field = defaultValue;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetLayoutStyleProp(LayoutStyleProperties flag)
    {
        _layoutStyleOverride |= flag;
    }

    [Flags]
    private enum LayoutStyleProperties : short
    {
        // @formatter:off
        None                = 0,
        Margin              = 1 << 0,
        MinWidth            = 1 << 1,
        MinHeight           = 1 << 2,
        SetWidth            = 1 << 3,
        SetHeight           = 1 << 4,
        MaxWidth            = 1 << 5,
        MaxHeight           = 1 << 6,
        StretchRatio        = 1 << 7,
        HorizontalExpand    = 1 << 8,
        VerticalExpand      = 1 << 9,
        HorizontalAlignment = 1 << 10,
        VerticalAlignment   = 1 << 11,
        // @formatter:on
    }
}
