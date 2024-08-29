﻿using System;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Input;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using static Robust.Client.UserInterface.Controls.LayoutContainer;

namespace Robust.Client.UserInterface.Controls
{
    [Virtual]
    public class Slider : Range
    {
        public const string StylePropertyBackground = "background";
        public const string StylePropertyForeground = "foreground";
        public const string StylePropertyFill = "fill";
        public const string StylePropertyGrabber = "grabber";

        public event Action<Slider>? OnGrabbed;
        public event Action<Slider>? OnReleased;

        protected readonly PanelContainer _foregroundPanel;
        protected readonly PanelContainer _backgroundPanel;
        protected readonly PanelContainer _fillPanel;
        protected readonly PanelContainer _grabber;

        private bool _grabbed;

        private StyleBox? _backgroundStyleBoxOverride;
        private StyleBox? _foregroundStyleBoxOverride;
        private StyleBox? _fillStyleBoxOverride;
        private StyleBox? _grabberStyleBoxOverride;

        public bool Grabbed => _grabbed;

        /// <summary>
        /// Whether the slider can be adjusted.
        /// </summary>
        public bool Disabled { get; set; }

        public StyleBox? ForegroundStyleBoxOverride
        {
            get => _foregroundStyleBoxOverride;
            set
            {
                _foregroundStyleBoxOverride = value;
                UpdateStyleBoxes();
            }
        }

        public StyleBox? BackgroundStyleBoxOverride
        {
            get => _backgroundStyleBoxOverride;
            set
            {
                _backgroundStyleBoxOverride = value;
                UpdateStyleBoxes();
            }
        }

        public StyleBox? FillStyleBoxOverride
        {
            get => _fillStyleBoxOverride;
            set
            {
                _fillStyleBoxOverride = value;
                UpdateStyleBoxes();
            }
        }

        public StyleBox? GrabberStyleBoxOverride
        {
            get => _grabberStyleBoxOverride;
            set
            {
                _grabberStyleBoxOverride = value;
                UpdateStyleBoxes();
            }
        }

        public Slider()
        {
            MouseFilter = MouseFilterMode.Stop;

            AddChild(new LayoutContainer
            {
                Children =
                {
                    (_backgroundPanel = new PanelContainer()),
                    (_fillPanel = new PanelContainer()),
                    (_foregroundPanel = new PanelContainer()),
                    (_grabber = new PanelContainer())
                }
            });

            SetAnchorBottom(_fillPanel, 1);
            SetAnchorBottom(_grabber, 1);
            SetGrowHorizontal(_grabber, GrowDirection.Both);
            SetGrowVertical(_grabber, GrowDirection.Both);
            SetAnchorPreset(_foregroundPanel, LayoutPreset.Wide);
            SetAnchorPreset(_backgroundPanel, LayoutPreset.Wide);
        }

        public override float Value
        {
            get => base.Value;
            set
            {
                base.Value = value;
                UpdateValue();
            }
        }

        public override void SetValueWithoutEvent(float newValue)
        {
            base.SetValueWithoutEvent(newValue);
            UpdateValue();
        }

        private void UpdateValue()
        {
            var ratio = GetAsRatio();

            var margin = (Width - _grabber.DesiredSize.X) * ratio + _grabber.DesiredSize.X / 2;
            SetMarginRight(_fillPanel, margin);
            SetMarginLeft(_grabber, margin);
            SetMarginRight(_grabber, margin);
        }

        protected override void Resized()
        {
            base.Resized();

            UpdateValue();
        }

        protected internal override void KeyBindDown(GUIBoundKeyEventArgs args)
        {
            base.KeyBindDown(args);

            if (args.Function != EngineKeyFunctions.UIClick || Disabled)
            {
                return;
            }

            HandlePositionChange(args.RelativePosition);
            _grabbed = true;
            OnGrabbed?.Invoke(this);
        }

        protected internal override void KeyBindUp(GUIBoundKeyEventArgs args)
        {
            base.KeyBindUp(args);

            if (args.Function != EngineKeyFunctions.UIClick || !_grabbed) return;

            _grabbed = false;
            OnReleased?.Invoke(this);
        }

        protected internal override void MouseMove(GUIMouseMoveEventArgs args)
        {
            if (!_grabbed)
            {
                return;
            }

            HandlePositionChange(args.RelativePosition);
        }

        private void HandlePositionChange(Vector2 position)
        {
            var grabberWidth = _grabber.DesiredSize.X;
            var ratio = (position.X - grabberWidth / 2) / (Width - grabberWidth);
            SetAsRatio(ratio);
        }

        protected override void StylePropertiesChanged()
        {
            base.StylePropertiesChanged();

            UpdateStyleBoxes();
        }

        protected virtual void UpdateStyleBoxes()
        {
            StyleBox? GetStyleBox(string name)
            {
                if (TryGetStyleProperty<StyleBox>(name, out var box))
                {
                    return box;
                }

                return null;
            }

            _backgroundPanel.PanelOverride = BackgroundStyleBoxOverride ?? GetStyleBox(StylePropertyBackground);
            _foregroundPanel.PanelOverride = ForegroundStyleBoxOverride ?? GetStyleBox(StylePropertyForeground);
            _fillPanel.PanelOverride = FillStyleBoxOverride ?? GetStyleBox(StylePropertyFill);
            _grabber.PanelOverride = GrabberStyleBoxOverride ?? GetStyleBox(StylePropertyGrabber);
        }
    }
}
