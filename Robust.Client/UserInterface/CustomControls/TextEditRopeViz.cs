using System;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.CustomControls;

internal sealed class TextEditRopeViz : OSWindow
{
    private static readonly Color[] LeafColors = CalcLeafColors();

    private readonly TextEdit _textEdit;

    private Vector2 _panOffset;

    private bool _dragging;
    private Vector2 _dragStartOffset;
    private Vector2 _dragStartMouse;

    public TextEditRopeViz(TextEdit textEdit)
    {
        _textEdit = textEdit;

        MouseFilter = MouseFilterMode.Stop;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!_textEdit.IsInsideTree)
            UserInterfaceManager.DeferAction(Close);
    }

    protected internal override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Function == EngineKeyFunctions.UIClick)
        {
            _dragStartOffset = _panOffset;
            _dragStartMouse = args.RelativePosition;
            _dragging = true;

            args.Handle();
        }
    }

    protected internal override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);

        if (args.Function == EngineKeyFunctions.UIClick)
        {
            _dragging = false;

            args.Handle();
        }
    }

    protected internal override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);

        if (!_dragging)
            return;

        _panOffset = args.RelativePosition - _dragStartMouse + _dragStartOffset;
    }

    protected override void Shown()
    {
        base.Shown();

        Root!.Name = nameof(TextEditRopeViz);
    }

    protected internal override void Draw(DrawingHandleScreen handle)
    {
        var root = _textEdit.TextRope;

        var totalDepth = root.Depth;

        DrawNode(root, _panOffset, 0, out _);

        const int nodeWidthHalf = 2;

        float DrawNode(Rope.Node node, Vector2 offset, int depth, out Vector2 nodePos)
        {
            switch (node)
            {
                case Rope.Branch branch:
                {
                    var depthOffset = 20 + (totalDepth - depth) * 4;
                    var leftWidth = DrawNode(branch.Left, offset + (0, depthOffset), depth + 1, out var leftPos);
                    var rightWidth = 0f;
                    Vector2? rightPos = null;
                    if (branch.Right is { } right)
                    {
                        rightWidth = DrawNode(right, offset + (leftWidth, depthOffset), depth + 1, out var rightPosOut);
                        rightPos = rightPosOut;
                    }

                    nodePos = offset + (leftWidth, 0);
                    handle.DrawLine(nodePos, leftPos, Color.DarkGray);

                    if (rightPos is { } rp)
                    {
                        handle.DrawLine(nodePos, rp, Color.DarkGray);
                    }
                    else
                    {
                        handle.DrawLine(nodePos, nodePos + (10, 10), Color.Red);
                    }

                    handle.DrawRect(new UIBox2(nodePos - (1, 1), nodePos + (2, 2)), Color.White);

                    return leftWidth + rightWidth;
                }
                case Rope.Leaf leaf:
                {
                    var colorIdx = leaf.Text.Length;
                    var color = colorIdx < LeafColors.Length ? LeafColors[colorIdx] : LeafColors[^1];
                    handle.DrawRect(new UIBox2(offset - (2, 2), offset + (3, 3)), color);
                    nodePos = offset;
                    return 6;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(node));
            }
        }

        static UIBox2 Around(Vector2 vec, float size)
        {
            return new UIBox2(vec - (size, size), vec + (size, size));
        }
    }

    private static Color[] CalcLeafColors()
    {
        var colors = new Color[21];
        colors[0] = Color.Purple;

        InterpColors(colors.AsSpan(1, 10), Color.Red, Color.Lime);
        InterpColors(colors.AsSpan(10, 10), Color.Red, Color.Lime);

        static void InterpColors(Span<Color> colors, Color α, Color β)
        {
            α = Color.FromSrgb(α);
            β = Color.FromSrgb(β);

            for (var i = 0; i < colors.Length; i++)
            {
                var λ = (float)i / (colors.Length - 1);

                var color = Color.InterpolateBetween(α, β, λ);

                colors[i] = Color.ToSrgb(color);
            }
        }

        return colors;
    }
}
