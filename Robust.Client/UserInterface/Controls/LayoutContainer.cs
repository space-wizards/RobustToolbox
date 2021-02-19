using System;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Robust.Client.UserInterface.Controls
{
    [PublicAPI]
    public class LayoutContainer : Container
    {
        /// <summary>
        ///     The value of an anchor that is exactly on the begin of the parent control.
        /// </summary>
        public const float AnchorBegin = 0;

        /// <summary>
        ///     The value of an anchor that is exactly on the end of the parent control.
        /// </summary>
        public const float AnchorEnd = 1;

        [ViewVariables(VVAccess.ReadWrite)] public bool Debug { get; set; }

        public static readonly AttachedProperty MarginLeftProperty = AttachedProperty.Create("MarginLeft",
            typeof(LayoutContainer), typeof(float), changed: LayoutPropertyChangedCallback);

        public static readonly AttachedProperty MarginTopProperty = AttachedProperty.Create("MarginTop",
            typeof(LayoutContainer), typeof(float), changed: LayoutPropertyChangedCallback);

        public static readonly AttachedProperty MarginRightProperty = AttachedProperty.Create("MarginRight",
            typeof(LayoutContainer), typeof(float), changed: LayoutPropertyChangedCallback);

        public static readonly AttachedProperty MarginBottomProperty = AttachedProperty.Create("MarginBottom",
            typeof(LayoutContainer), typeof(float), changed: LayoutPropertyChangedCallback);

        public static readonly AttachedProperty AnchorLeftProperty = AttachedProperty.Create("AnchorLeft",
            typeof(LayoutContainer), typeof(float), changed: LayoutPropertyChangedCallback);

        public static readonly AttachedProperty AnchorTopProperty = AttachedProperty.Create("AnchorTop",
            typeof(LayoutContainer), typeof(float), changed: LayoutPropertyChangedCallback);

        public static readonly AttachedProperty AnchorRightProperty = AttachedProperty.Create("AnchorRight",
            typeof(LayoutContainer), typeof(float), changed: LayoutPropertyChangedCallback);

        public static readonly AttachedProperty AnchorBottomProperty = AttachedProperty.Create("AnchorBottom",
            typeof(LayoutContainer), typeof(float), changed: LayoutPropertyChangedCallback);

        public static readonly AttachedProperty GrowHorizontalProperty = AttachedProperty.Create("GrowHorizontal",
            typeof(LayoutContainer), typeof(GrowDirection), changed: LayoutPropertyChangedCallback);

        public static readonly AttachedProperty GrowVerticalProperty = AttachedProperty.Create("GrowVertical",
            typeof(LayoutContainer), typeof(GrowDirection), changed: LayoutPropertyChangedCallback);


        public static void SetMarginLeft(Control control, float value)
        {
            control.SetValue(MarginLeftProperty, value);
        }

        public static void SetMarginTop(Control control, float value)
        {
            control.SetValue(MarginTopProperty, value);
        }

        public static void SetMarginRight(Control control, float value)
        {
            control.SetValue(MarginRightProperty, value);
        }

        public static void SetMarginBottom(Control control, float value)
        {
            control.SetValue(MarginBottomProperty, value);
        }

        public static void SetAnchorLeft(Control control, float value)
        {
            control.SetValue(AnchorLeftProperty, value);
        }

        public static void SetAnchorTop(Control control, float value)
        {
            control.SetValue(AnchorTopProperty, value);
        }

        public static void SetAnchorRight(Control control, float value)
        {
            control.SetValue(AnchorRightProperty, value);
        }

        public static void SetAnchorBottom(Control control, float value)
        {
            control.SetValue(AnchorBottomProperty, value);
        }

        public static void SetGrowHorizontal(Control control, GrowDirection value)
        {
            control.SetValue(GrowHorizontalProperty, value);
        }

        public static void SetGrowVertical(Control control, GrowDirection value)
        {
            control.SetValue(GrowVerticalProperty, value);
        }

        public static void SetPosition(Control control, Vector2 position)
        {
            var (diffX, diffY) = position - control.Position;

            // This is just to make subsequent set calls work correctly.
            // It should get reset to this exact value next update either way.
            control.Position = position;

            SetMarginLeft(control, diffX + control.GetValue<float>(MarginLeftProperty));
            SetMarginTop(control, diffY + control.GetValue<float>(MarginTopProperty));
            SetMarginRight(control, diffX + control.GetValue<float>(MarginRightProperty));
            SetMarginBottom(control, diffY + control.GetValue<float>(MarginBottomProperty));
        }

        [Obsolete("Change SetSize on the control instead.")]
        public new static void SetSize(Control control, Vector2 size)
        {
            control.SetSize = size;
        }

        /// <summary>
        ///     Sets an anchor AND a margin preset. This is most likely the method you want.
        ///

        /// </summary>
        /// <remarks>
        ///     Note that the current size and minimum size of the control affects how
        ///     each of the margins will be set, so if your control needs to shrink beyond its
        ///     current size / min size, you should either not call this method or only call it when your
        ///     control has a size of (0, 0). Otherwise your control's size will never be able
        ///     to go below the size implied by the margins set in this method.
        /// </remarks>
        public static void SetAnchorAndMarginPreset(Control control, LayoutPreset preset,
            LayoutPresetMode mode = LayoutPresetMode.MinSize,
            int margin = 0)
        {
            SetAnchorPreset(control, preset);
            SetMarginsPreset(control, preset, mode, margin);
        }

        /// <summary>
        ///     Changes all the anchors of a node at once to common presets.
        ///     The result is that the anchors are laid out to be suitable for a preset.
        /// </summary>
        /// <param name="preset">
        ///     The preset to apply to the anchors.
        /// </param>
        /// <param name="keepMargin">
        ///     If this is true, the control margin values themselves will not be changed,
        ///     and the control position and size will change according to the new anchor parameters.
        ///     If false, the control margins will adjust so that the control position and size remains the same relative to its parent.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown if <paramref name="preset" /> isn't a valid preset value.
        /// </exception>
        public static void SetAnchorPreset(Control control, LayoutPreset preset, bool keepMargin = false)
        {
            // TODO: Implement keepMargin.

            // Left Anchor.
            switch (preset)
            {
                case LayoutPreset.TopLeft:
                case LayoutPreset.BottomLeft:
                case LayoutPreset.CenterLeft:
                case LayoutPreset.LeftWide:
                case LayoutPreset.HorizontalCenterWide:
                case LayoutPreset.Wide:
                case LayoutPreset.TopWide:
                case LayoutPreset.BottomWide:
                    control.SetValue(AnchorLeftProperty, 0f);
                    break;
                case LayoutPreset.CenterTop:
                case LayoutPreset.CenterBottom:
                case LayoutPreset.Center:
                case LayoutPreset.VerticalCenterWide:
                    control.SetValue(AnchorLeftProperty, 0.5f);
                    break;
                case LayoutPreset.TopRight:
                case LayoutPreset.BottomRight:
                case LayoutPreset.CenterRight:
                case LayoutPreset.RightWide:
                    control.SetValue(AnchorLeftProperty, 1f);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(preset), preset, null);
            }

            // Top Anchor.
            switch (preset)
            {
                case LayoutPreset.TopLeft:
                case LayoutPreset.TopRight:
                case LayoutPreset.LeftWide:
                case LayoutPreset.TopWide:
                case LayoutPreset.Wide:
                case LayoutPreset.RightWide:
                case LayoutPreset.CenterTop:
                case LayoutPreset.VerticalCenterWide:
                    control.SetValue(AnchorTopProperty, 0f);
                    break;
                case LayoutPreset.CenterLeft:
                case LayoutPreset.CenterRight:
                case LayoutPreset.HorizontalCenterWide:
                case LayoutPreset.Center:
                    control.SetValue(AnchorTopProperty, 0.5f);
                    break;
                case LayoutPreset.CenterBottom:
                case LayoutPreset.BottomLeft:
                case LayoutPreset.BottomRight:
                case LayoutPreset.BottomWide:
                    control.SetValue(AnchorTopProperty, 1f);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(preset), preset, null);
            }

            // Right Anchor.
            switch (preset)
            {
                case LayoutPreset.TopLeft:
                case LayoutPreset.CenterLeft:
                case LayoutPreset.BottomLeft:
                case LayoutPreset.LeftWide:
                    control.SetValue(AnchorRightProperty, 0f);
                    break;
                case LayoutPreset.CenterTop:
                case LayoutPreset.CenterBottom:
                case LayoutPreset.Center:
                case LayoutPreset.VerticalCenterWide:
                    control.SetValue(AnchorRightProperty, 0.5f);
                    break;
                case LayoutPreset.CenterRight:
                case LayoutPreset.TopRight:
                case LayoutPreset.Wide:
                case LayoutPreset.HorizontalCenterWide:
                case LayoutPreset.TopWide:
                case LayoutPreset.BottomWide:
                case LayoutPreset.RightWide:
                case LayoutPreset.BottomRight:
                    control.SetValue(AnchorRightProperty, 1f);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(preset), preset, null);
            }

            // Bottom Anchor.
            switch (preset)
            {
                case LayoutPreset.TopWide:
                case LayoutPreset.TopLeft:
                case LayoutPreset.TopRight:
                case LayoutPreset.CenterTop:
                    control.SetValue(AnchorBottomProperty, 0f);
                    break;
                case LayoutPreset.CenterLeft:
                case LayoutPreset.CenterRight:
                case LayoutPreset.Center:
                case LayoutPreset.HorizontalCenterWide:
                    control.SetValue(AnchorBottomProperty, 0.5f);
                    break;
                case LayoutPreset.CenterBottom:
                case LayoutPreset.BottomLeft:
                case LayoutPreset.BottomRight:
                case LayoutPreset.LeftWide:
                case LayoutPreset.Wide:
                case LayoutPreset.RightWide:
                case LayoutPreset.VerticalCenterWide:
                case LayoutPreset.BottomWide:
                    control.SetValue(AnchorBottomProperty, 1f);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(preset), preset, null);
            }
        }

        /// <summary>
        ///     Changes all the margins of a control at once to common presets.
        ///     The result is that the control is laid out as specified by the preset.
        ///
        ///     Note that the current size and minimum size of the control affects how
        ///     each of the margins will be set, so if your control needs to shrink beyond its
        ///     current size / min size, you should either not call this method or only call it when your
        ///     control has a size of (0, 0). Otherwise your control's size will never be able
        ///     to go below the size implied by the margins set in this method.
        /// </summary>
        /// <param name="preset"></param>
        /// <param name="resizeMode"></param>
        /// <param name="margin">Some extra margin to add depending on the preset chosen.</param>
        public static void SetMarginsPreset(Control control, LayoutPreset preset,
            LayoutPresetMode resizeMode = LayoutPresetMode.MinSize,
            int margin = 0)
        {
            control.Measure(Vector2.Infinity);
            var newSize = control.Size;
            var minSize = control.DesiredSize;
            if ((resizeMode & LayoutPresetMode.KeepWidth) == 0)
            {
                newSize = new Vector2(minSize.X, newSize.Y);
            }

            if ((resizeMode & LayoutPresetMode.KeepHeight) == 0)
            {
                newSize = new Vector2(newSize.X, minSize.Y);
            }

            var parentSize = control.Parent?.Size ?? Vector2.Zero;

            var anchorLeft = control.GetValue<float>(AnchorLeftProperty);
            var anchorTop = control.GetValue<float>(AnchorBottomProperty);
            var anchorRight = control.GetValue<float>(AnchorRightProperty);
            var anchorBottom = control.GetValue<float>(AnchorBottomProperty);

            float marginLeft;
            float marginTop;
            float marginRight;
            float marginBottom;

            // Left Margin.
            switch (preset)
            {
                case LayoutPreset.TopLeft:
                case LayoutPreset.BottomLeft:
                case LayoutPreset.CenterLeft:
                case LayoutPreset.LeftWide:
                case LayoutPreset.HorizontalCenterWide:
                case LayoutPreset.Wide:
                case LayoutPreset.TopWide:
                case LayoutPreset.BottomWide:
                    // The AnchorLeft bit is to reverse the effect of anchors,
                    // So that the preset result is the same no matter what margins are set.
                    marginLeft = parentSize.X * (0 - anchorLeft) + margin;
                    break;
                case LayoutPreset.CenterTop:
                case LayoutPreset.CenterBottom:
                case LayoutPreset.Center:
                case LayoutPreset.VerticalCenterWide:
                    marginLeft = parentSize.X * (0.5f - anchorLeft) - newSize.X / 2;
                    break;
                case LayoutPreset.TopRight:
                case LayoutPreset.BottomRight:
                case LayoutPreset.CenterRight:
                case LayoutPreset.RightWide:
                    marginLeft = parentSize.X * (1 - anchorLeft) - newSize.X - margin;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(preset), preset, null);
            }

            // Top Anchor.
            switch (preset)
            {
                case LayoutPreset.TopLeft:
                case LayoutPreset.TopRight:
                case LayoutPreset.LeftWide:
                case LayoutPreset.TopWide:
                case LayoutPreset.Wide:
                case LayoutPreset.RightWide:
                case LayoutPreset.CenterTop:
                case LayoutPreset.VerticalCenterWide:
                    marginTop = parentSize.Y * (0 - anchorTop) + margin;
                    break;
                case LayoutPreset.CenterLeft:
                case LayoutPreset.CenterRight:
                case LayoutPreset.HorizontalCenterWide:
                case LayoutPreset.Center:
                    marginTop = parentSize.Y * (0.5f - anchorTop) - newSize.Y / 2;
                    break;
                case LayoutPreset.CenterBottom:
                case LayoutPreset.BottomLeft:
                case LayoutPreset.BottomRight:
                case LayoutPreset.BottomWide:
                    marginTop = parentSize.Y * (1 - anchorTop) - newSize.Y - margin;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(preset), preset, null);
            }

            // Right Anchor.
            switch (preset)
            {
                case LayoutPreset.TopLeft:
                case LayoutPreset.CenterLeft:
                case LayoutPreset.BottomLeft:
                case LayoutPreset.LeftWide:
                    marginRight = parentSize.X * (0 - anchorRight) + newSize.X + margin;
                    break;
                case LayoutPreset.CenterTop:
                case LayoutPreset.CenterBottom:
                case LayoutPreset.Center:
                case LayoutPreset.VerticalCenterWide:
                    marginRight = parentSize.X * (0.5f - anchorRight) + newSize.X;
                    break;
                case LayoutPreset.CenterRight:
                case LayoutPreset.TopRight:
                case LayoutPreset.Wide:
                case LayoutPreset.HorizontalCenterWide:
                case LayoutPreset.TopWide:
                case LayoutPreset.BottomWide:
                case LayoutPreset.RightWide:
                case LayoutPreset.BottomRight:
                    marginRight = parentSize.X * (1 - anchorRight) - margin;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(preset), preset, null);
            }

            // Bottom Anchor.
            switch (preset)
            {
                case LayoutPreset.TopWide:
                case LayoutPreset.TopLeft:
                case LayoutPreset.TopRight:
                case LayoutPreset.CenterTop:
                    marginBottom = parentSize.Y * (0 - anchorBottom) + newSize.Y + margin;
                    break;
                case LayoutPreset.CenterLeft:
                case LayoutPreset.CenterRight:
                case LayoutPreset.Center:
                case LayoutPreset.HorizontalCenterWide:
                    marginBottom = parentSize.Y * (0.5f - anchorBottom) + newSize.Y;
                    break;
                case LayoutPreset.CenterBottom:
                case LayoutPreset.BottomLeft:
                case LayoutPreset.BottomRight:
                case LayoutPreset.LeftWide:
                case LayoutPreset.Wide:
                case LayoutPreset.RightWide:
                case LayoutPreset.VerticalCenterWide:
                case LayoutPreset.BottomWide:
                    marginBottom = parentSize.Y * (1 - anchorBottom) - margin;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(preset), preset, null);
            }

            control.SetValue(MarginLeftProperty, marginLeft);
            control.SetValue(MarginTopProperty, marginTop);
            control.SetValue(MarginRightProperty, marginRight);
            control.SetValue(MarginBottomProperty, marginBottom);
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            base.MeasureOverride(Vector2.Infinity);

            return Vector2.Zero;
        }

        protected override Vector2 ArrangeOverride(Vector2 finalSize)
        {
            foreach (var child in Children)
            {
                child.Arrange(CalcChildRect(finalSize, UIScale, child, out _));
            }

            return finalSize;
        }

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            if (!Debug)
                return;

            var (pSizeX, pSizeY) = PixelSize;
            foreach (var child in Children)
            {
                var rect = CalcChildRect(Size, UIScale, child, out var anchorSize);

                var left = rect.Left * UIScale;
                var right = rect.Right * UIScale;
                var top = rect.Top * UIScale;
                var bottom = rect.Bottom * UIScale;

                DrawVLine(anchorSize.Left, Color.Pink);
                DrawVLine(anchorSize.Right, Color.Green);
                DrawHLine(anchorSize.Top, Color.Pink);
                DrawHLine(anchorSize.Bottom, Color.Green);

                /*
                DrawVLine(left, Color.Orange);
                DrawVLine(right, Color.Blue);
                DrawHLine(top, Color.Orange);
                DrawHLine(bottom, Color.Blue);
                */

                handle.DrawRect(new UIBox2(left, top, right, bottom), Color.Red, false);
            }

            void DrawVLine(float x, Color color)
            {
                handle.DrawLine((x, 0), (x, pSizeY), color);
            }

            void DrawHLine(float y, Color color)
            {
                handle.DrawLine((0, y), (pSizeX, y), color);
            }
        }

        private static UIBox2 CalcChildRect(Vector2 ourSize, float uiScale, Control child, out UIBox2 anchorSize)
        {
            var (pSizeX, pSizeY) = ourSize * uiScale;

            var anchorLeft = child.GetValue<float>(AnchorLeftProperty);
            var anchorTop = child.GetValue<float>(AnchorTopProperty);
            var anchorRight = child.GetValue<float>(AnchorRightProperty);
            var anchorBottom = child.GetValue<float>(AnchorBottomProperty);

            var marginLeft = child.GetValue<float>(MarginLeftProperty) * uiScale;
            var marginTop = child.GetValue<float>(MarginTopProperty) * uiScale;
            var marginRight = child.GetValue<float>(MarginRightProperty) * uiScale;
            var marginBottom = child.GetValue<float>(MarginBottomProperty) * uiScale;

            var growHorizontal = child.GetValue<GrowDirection>(GrowHorizontalProperty);
            var growVertical = child.GetValue<GrowDirection>(GrowVerticalProperty);

            // Calculate where the control "wants" to be by its anchors/margins.
            var left = anchorLeft * pSizeX + marginLeft;
            var top = anchorTop * pSizeY + marginTop;
            var right = anchorRight * pSizeX + marginRight;
            var bottom = anchorBottom * pSizeY + marginBottom;

            anchorSize = new UIBox2(left, top, right, bottom);

            var (wSizeX, wSizeY) = (right - left, bottom - top);
            var (minSizeX, minSizeY) = child.DesiredPixelSize;

            HandleLayoutOverflow(growHorizontal, minSizeX, left, wSizeX, out var posX, out var sizeX);
            HandleLayoutOverflow(growVertical, minSizeY, top, wSizeY, out var posY, out var sizeY);

            return UIBox2.FromDimensions(posX / uiScale, posY / uiScale, sizeX / uiScale, sizeY / uiScale);
        }

        private static void HandleLayoutOverflow(GrowDirection direction, float minSize, float wPos, float wSize,
            out float pos,
            out float size)
        {
            var overflow = minSize - wSize;
            if (overflow <= 0)
            {
                pos = wPos;
                size = wSize;
                return;
            }

            switch (direction)
            {
                case GrowDirection.End:
                    pos = wPos;
                    break;
                case GrowDirection.Begin:
                    pos = wPos - overflow;
                    break;
                case GrowDirection.Both:
                    pos = wPos - overflow / 2;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            size = minSize;
        }

        private static void LayoutPropertyChangedCallback(Control owner, AttachedPropertyChangedEventArgs eventArgs)
        {
            if (owner.Parent is LayoutContainer container)
            {
                container.InvalidateArrange();
            }
        }

        /// <summary>
        ///     Controls how the control should move when its wanted size (controlled by anchors/margins) is smaller
        ///     than its minimum size.
        /// </summary>
        public enum GrowDirection : byte
        {
            /// <summary>
            ///     The control will expand to the bottom right to reach its minimum size.
            /// </summary>
            End = 0,

            /// <summary>
            ///     The control will expand to the top left to reach its minimum size.
            /// </summary>
            Begin,

            /// <summary>
            ///     The control will expand on all axes equally to reach its minimum size.
            /// </summary>
            Both
        }

        /// <seealso cref="Control.SetMarginsPreset" />
        [Flags]
        [PublicAPI]
        public enum LayoutPresetMode : byte
        {
            /// <summary>
            ///     Reset control size to minimum size.
            /// </summary>
            MinSize = 0,

            /// <summary>
            ///     Reset height to minimum but keep width the same.
            /// </summary>
            KeepWidth = 1,

            /// <summary>
            ///     Reset width to minimum but keep height the same.
            /// </summary>
            KeepHeight = 2,

            /// <summary>
            ///     Do not modify control size at all.
            /// </summary>
            KeepSize = KeepWidth | KeepHeight,
        }

        public enum LayoutPreset : byte
        {
            TopLeft = 0,
            TopRight = 1,
            BottomLeft = 2,
            BottomRight = 3,
            CenterLeft = 4,
            CenterTop = 5,
            CenterRight = 6,
            CenterBottom = 7,
            Center = 8,
            LeftWide = 9,
            TopWide = 10,
            RightWide = 11,
            BottomWide = 12,
            VerticalCenterWide = 13,
            HorizontalCenterWide = 14,
            Wide = 15,
        }
    }
}
