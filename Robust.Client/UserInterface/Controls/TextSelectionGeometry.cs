using System;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Maths;
using System.Collections.Generic;

namespace Robust.Client.UserInterface.Controls;

/// <summary>
/// Shared helpers for text selection hit-testing and rendering.
/// </summary>
internal static class TextSelectionGeometry
{
    /// <summary>
    /// Chooses the closest UTF-16 index for a point located between two glyph bounds.
    /// </summary>
    public static int ChooseNearestIndex(
        float targetX,
        float leftX,
        float rightX,
        int lineStartIndex,
        int leftIndex,
        int rightIndex)
    {
        var distanceRight = rightX - targetX;
        var distanceLeft = targetX - leftX;
        if (distanceRight > distanceLeft && leftIndex > lineStartIndex)
            return leftIndex;

        return rightIndex;
    }

    /// <summary>
    /// Generic map of text selection geometry for hit-testing and selection rendering.
    /// </summary>
    public sealed class SelectionMap
    {
        private readonly List<Line> _lines = [];
        private Line? _currentLine;

        /// <summary>
        /// Returns <see langword="true"/> when the map has no completed lines.
        /// </summary>
        public bool IsEmpty => _lines.Count == 0;

        /// <summary>
        /// Starts collecting geometry for a new line.
        /// </summary>
        /// <param name="startIndex">UTF-16 index at the start of the line.</param>
        /// <param name="startX">X coordinate of the line start in draw-space pixels.</param>
        /// <param name="top">Top Y coordinate of the line rectangle.</param>
        /// <param name="bottom">Bottom Y coordinate of the line rectangle.</param>
        public void BeginLine(int startIndex, float startX, float top, float bottom)
        {
            _currentLine = new Line(startIndex, startX, top, bottom);
            _currentLine.AddBoundary(startIndex, startX);
        }

        /// <summary>
        /// Adds an index-to-X boundary point to the current line.
        /// </summary>
        /// <param name="index">UTF-16 index represented by the boundary.</param>
        /// <param name="x">X coordinate in draw-space pixels.</param>
        public void AddBoundary(int index, float x)
        {
            _currentLine?.AddBoundary(index, x);
        }

        /// <summary>
        /// Finalizes the current line and stores it in the map.
        /// </summary>
        /// <param name="endIndex">UTF-16 index at the visual end of the line.</param>
        /// <param name="endX">X coordinate of the visual line end.</param>
        public void EndLine(int endIndex, float endX)
        {
            if (_currentLine == null)
                return;

            _currentLine.EndIndex = endIndex;
            _currentLine.EndX = endX;
            _currentLine.AddBoundary(endIndex, endX);
            _lines.Add(_currentLine);
            _currentLine = null;
        }

        /// <summary>
        /// Resolves a draw-space position to the nearest UTF-16 index.
        /// </summary>
        /// <param name="position">Position in draw-space pixels.</param>
        /// <returns>Nearest index for caret placement / hit-testing.</returns>
        public int GetIndexAtPosition(Vector2 position)
        {
            if (_lines.Count == 0)
                return 0;

            var line = GetLineForY(position.Y);

            if (position.X <= line.StartX)
                return line.StartIndex;

            if (position.X >= line.EndX)
                return line.EndIndex;

            return line.GetIndexAtX(position.X);
        }

        /// <summary>
        /// Draws highlight rectangles for the requested index range.
        /// </summary>
        /// <param name="handle">Screen drawing handle.</param>
        /// <param name="selectionLower">Lower UTF-16 selection bound.</param>
        /// <param name="selectionUpper">Upper UTF-16 selection bound.</param>
        /// <param name="color">Highlight color.</param>
        public void DrawSelection(DrawingHandleScreen handle, int selectionLower, int selectionUpper, Color color)
        {
            if (selectionUpper <= selectionLower)
                return;

            foreach (var line in _lines)
            {
                var start = Math.Max(selectionLower, line.StartIndex);
                var end = Math.Min(selectionUpper, line.EndIndex);
                if (end <= start)
                    continue;

                var left = line.GetXForIndex(start);
                var right = line.GetXForIndex(end);
                if (right <= left)
                    continue;

                handle.DrawRect(new UIBox2(left, line.Top, right, line.Bottom), color);
            }
        }

        /// <summary>
        /// Picks a line by Y coordinate, clamped to first/last line when outside.
        /// </summary>
        /// <param name="y">Draw-space Y coordinate.</param>
        /// <returns>Matching line geometry record.</returns>
        private Line GetLineForY(float y)
        {
            if (y <= _lines[0].Top)
                return _lines[0];

            for (var i = 0; i < _lines.Count; i++)
            {
                var line = _lines[i];
                if (y <= line.Bottom)
                    return line;
            }

            return _lines[^1];
        }

        private sealed class Line(int startIndex, float startX, float top, float bottom)
        {
            private readonly List<int> _indices = [];
            private readonly List<float> _positions = [];

            public int StartIndex { get; } = startIndex;
            public int EndIndex { get; set; } = startIndex;
            public float StartX { get; } = startX;
            public float EndX { get; set; } = startX;
            public float Top { get; } = top;
            public float Bottom { get; } = bottom;

            /// <summary>
            /// Adds a boundary if it is not a duplicate of the previous one.
            /// </summary>
            /// <param name="index">UTF-16 index for this boundary.</param>
            /// <param name="x">Draw-space X coordinate for this boundary.</param>
            public void AddBoundary(int index, float x)
            {
                if (_indices.Count > 0 &&
                    _indices[^1] == index &&
                    MathHelper.CloseToPercent(_positions[^1], x))
                {
                    return;
                }

                _indices.Add(index);
                _positions.Add(x);
            }

            /// <summary>
            /// Resolves the nearest UTF-16 index for an X coordinate on this line.
            /// </summary>
            /// <param name="x">Draw-space X coordinate.</param>
            /// <returns>Nearest index on this line.</returns>
            public int GetIndexAtX(float x)
            {
                if (_indices.Count == 0)
                    return StartIndex;

                if (x <= _positions[0])
                    return _indices[0];

                for (var i = 1; i < _indices.Count; i++)
                {
                    if (x > _positions[i])
                        continue;

                    return ChooseNearestIndex(
                        x,
                        _positions[i - 1],
                        _positions[i],
                        StartIndex,
                        _indices[i - 1],
                        _indices[i]);
                }

                return EndIndex;
            }

            /// <summary>
            /// Resolves an index to an X coordinate for highlight rectangle bounds.
            /// </summary>
            /// <param name="index">UTF-16 index.</param>
            /// <returns>Approximate draw-space X coordinate for the index.</returns>
            public float GetXForIndex(int index)
            {
                if (_indices.Count == 0)
                    return StartX;

                if (index <= _indices[0])
                    return _positions[0];

                for (var i = 1; i < _indices.Count; i++)
                {
                    if (_indices[i] >= index)
                        return _positions[i];
                }

                return EndX;
            }
        }
    }
}
