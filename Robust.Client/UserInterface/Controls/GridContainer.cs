using System;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    /// <summary>
    ///     A container that lays out its children in a grid. Can define specific count of
    ///     rows or specific count of columns (not both), and will grow to fill in additional rows/columns within
    ///     that limit. Alternatively, can define a maximum width or height, and grid will
    ///     lay out elements (aligned in a grid pattern, not floated) within the defined limit.
    /// </summary>
    public class GridContainer : Container
    {
        // limit - depending on mode, this is either rows or columns
        private int _limitedDimensionCount = 1;
        // virtual pixels
        private float _limitSize;
        private LimitType _limitType = LimitType.Count;
        private Dimension _limitDimension = Dimension.Column;


        /// <summary>
        /// Indicates whether row or column count has been specified, and thus
        /// how items will fill them out as they are added.
        /// This is set depending on whether you have specified Columns or Rows.
        /// </summary>
        public Dimension LimitedDimension => _limitDimension;
        /// <summary>
        /// Opposite dimension of LimitedDimension
        /// </summary>
        public Dimension UnlimitedDimension => _limitDimension == Dimension.Column ? Dimension.Row : Dimension.Column;

        /// <summary>
        /// Indicates whether we are limiting based on an explicit number of rows or columns, or limiting
        /// based on a defined max width or height.
        /// </summary>
        public LimitType LimitType => _limitType;

        /// <summary>
        /// The "normal" direction of expansion when the defined row or column limit is met
        /// is right (for row-limited) and down (for column-limited),
        /// this inverts that so the container expands in the opposite direction as elements are added.
        /// </summary>
        public bool ExpandBackwards
        {
            get => _expandBackwards;
            set
            {
                _expandBackwards = value;
                InvalidateArrange();
            }
        }
        private bool _expandBackwards;

        /// <summary>
        ///     The number of columns to organize the children into. Setting this puts this grid
        ///     into LimitMode.LimitColumns and LimitType.Count - items will be added to fill up the entire row, up to the defined
        ///     limit of columns, and then create a second row.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown if the value assigned is less than or equal to 0.
        /// </exception>
        /// <returns>specified limit if LimitMode.LimitColums, otherwise the number
        /// of columns being used for the current amount of children.</returns>
        public int Columns
        {
            get => GetCount(Dimension.Column);
            set => SetCount(Dimension.Column, value);
        }

        /// <summary>
        ///     The number of rows to organize the children into. Setting this puts this grid
        ///     into LimitMode.LimitRows and LimitType.Count - items will be added to fill up the entire column, up to the defined
        ///     limit of rows, and then create a second column.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown if the value assigned is less than or equal to 0.
        /// </exception>
        /// <returns>specified limit if LimitMode.LimitRows, otherwise the number
        /// of rows being used for the current number of children.</returns>
        public int Rows
        {
            get => GetCount(Dimension.Row);
            set => SetCount(Dimension.Row, value);
        }

        /// <summary>
        ///     The max width (in virtual pixels) the grid of elements can have. This dynamically determines
        ///     the number of columns based on the size of the elements. Setting this puts this grid
        ///     into LimitMode.LimitColumns and LimitType.Size. Items will be added to fill up the entire row, up to the defined
        ///     width, and then create a second row.
        ///
        ///     In the presence of unevenly-sized children,
        ///     rows will still have the same amount elements - the items are laid out in a grid pattern such
        ///     that they are all aligned, the height and width of each "cell" being determined by
        ///     the greatest min height and min width among the elements. In the presence of expanding elements,
        ///     their pre-expanded size will be used to determine the cell layout, then the elements expand within
        ///     the defined Control.Size
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown if the value assigned is less than or equal to 0.
        /// </exception>
        public float MaxGridWidth
        {
            set => SetMaxSize(Dimension.Column, value);
        }

        /// <summary>
        ///     The max height (in virtual pixels) the grid  of elements can have. This dynamically determines
        ///     the number of rows based on the size of the elements. Setting this puts this grid
        ///     into LimitMode.LimitRows and LimitType.Size - items will be added to fill up the entire column, up to the defined
        ///     height, and then create a second column.
        ///
        ///     In the presence of unevenly-sized children,
        ///     columns will still have the same amount elements - the items are laid out in a grid pattern such
        ///     that they are all aligned, the height and width of each "cell" being determined by
        ///     the greatest min height and min width among the elements. In the presence of expanding elements,
        ///     their pre-expanded size will be used to determine the layout, then the elements expand within
        ///     the defined Control.Size
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown if the value assigned is less than or equal to 0.
        /// </exception>
        public float MaxGridHeight
        {
            set => SetMaxSize(Dimension.Row, value);
        }

        private int? _vSeparationOverride;

        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        public int? VSeparationOverride
        {
            get => _vSeparationOverride;
            set => _vSeparationOverride = value;
        }

        private int? _hSeparationOverride;

        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        public int? HSeparationOverride
        {
            get => _hSeparationOverride;
            set => _hSeparationOverride = value;
        }

        private Vector2i Separations => (_hSeparationOverride ?? 4, _vSeparationOverride ?? 4);

        private float GetLimitPixelSize()
        {
            return _limitSize * UIScale;
        }

        private int GetCount(Dimension forDimension)
        {
            if (_limitType == LimitType.Count)
            {
                if (forDimension == _limitDimension) return _limitedDimensionCount;
                if (ChildCount == 0)
                {
                    return 1;
                }

                var divisor = (_limitDimension == Dimension.Column ? Columns : Rows);
                var div = ChildCount / divisor;
                if (ChildCount % divisor != 0)
                {
                    div += 1;
                }

                return div;
            }
            else
            {
                if (forDimension == _limitDimension) return CalculateLimitedCount();
                if (ChildCount == 0)
                {
                    return 1;
                }
                var divisor = CalculateLimitedCount();;
                var div = ChildCount / divisor;
                if (ChildCount % divisor != 0)
                {
                    div += 1;
                }

                return div;
            }
        }

        private void SetCount(Dimension forDimension, int value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Value must be greater than zero.");
            }

            _limitDimension = forDimension;
            _limitType = LimitType.Count;

            _limitedDimensionCount = value;
            InvalidateMeasure();
        }

        private void SetMaxSize(Dimension forDimension, float value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Value must be greater than zero.");
            }

            _limitDimension = forDimension;
            _limitType = LimitType.Size;

            _limitSize = value;
            InvalidateMeasure();
        }

        /// <summary>
        /// If columns (or width) are being limited, calculates how many columns
        /// there should be.
        /// </summary>
        private int CalculateLimitedCount()
        {
            if (_limitType == LimitType.Count) return _limitedDimensionCount;

            // to make it easier to read and visualize, we're just going to use the terms "x" and "y", width, and height,
            // rows and cols,
            // but at the start of the method here we'll set those to what they actually are based
            // on the limited dimension, which might involve swapping them.
            // For the below convention, we pretend that columns (or width) have a limit defined, thus
            // the amount of rows is not limited (unlimited).

            // we convert all elements to "cells" of the same size so they will align,
            // also converting to our pretend scenario of limited columns/width
            var (cellWidthActual, cellHeightActual) = CellSize();
            var (wSepActual, hSepActual) = (Vector2i) (Separations * UIScale);
            var cellWidth = _limitDimension == Dimension.Column ? cellWidthActual : cellHeightActual;
            var wSep = _limitDimension == Dimension.Column ? wSepActual : hSepActual;

            // calculate how many cells will fit into a given column without going over, accounting
            // for additional wSep between each cell only if there's more than one
            if (ChildCount == 0) return 1;

            if ((2 * cellWidth + wSep) > GetLimitPixelSize())
            {
                return 1;
            }

            return Math.Min(ChildCount, (int) ((GetLimitPixelSize() + wSep) / (cellWidth + wSep)));
        }

        /// <summary>
        /// Calculates the size of a "cell" in physical pixels, for use in LimitType.Size mode. This
        /// is based on the maximum minheight / minwidth of each element.
        /// </summary>
        /// <returns></returns>
        private Vector2i CellSize()
        {
            int maxMinWidth = -1;
            int maxMinHeight = -1;
            foreach (var child in Children)
            {
                var (minSizeX, minSizeY) = child.DesiredPixelSize;
                maxMinWidth = Math.Max(maxMinWidth, minSizeX);
                maxMinHeight = Math.Max(maxMinHeight, minSizeY);
            }

            return new Vector2i(maxMinWidth, maxMinHeight);
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            // to make it easier to read and visualize, we're just going to use the terms "x" and "y", width, and height,
            // rows and cols,
            // but at the start of the method here we'll set those to what they actually are based
            // on the limited dimension, which might involve swapping them.
            // For the below convention, we pretend that columns have a limit defined, thus
            // the amount of rows is not limited (unlimited).

            foreach (var child in Children)
            {
                // TODO: This is not really correct in any fucking way but I CBA to fix this properly.
                child.Measure(availableSize);
            }

            var rows = GetCount(UnlimitedDimension);
            var cols = GetCount(LimitedDimension);
            var cellSize = CellSize();

            Span<int> minColWidth = stackalloc int[cols];
            Span<int> minRowHeight = stackalloc int[rows];

            minColWidth.Fill(0);
            minRowHeight.Fill(0);

            var index = 0;
            foreach (var child in Children)
            {
                if (!child.Visible)
                {
                    index--;
                    continue;
                }

                var column = index % cols;
                var row = index / cols;

                // also converting here to our "pretend" scenario where columns have a limit defined.
                // note if we are limiting by size rather than count, the size of each child is constant (cell size)
                var (minSizeXActual, minSizeYActual) = _limitType == LimitType.Count ? child.DesiredPixelSize : cellSize;
                var minSizeX = _limitDimension == Dimension.Column ? minSizeXActual : minSizeYActual;
                var minSizeY = _limitDimension == Dimension.Column ? minSizeYActual : minSizeXActual;
                minColWidth[column] = Math.Max(minSizeX, minColWidth[column]);
                minRowHeight[row] = Math.Max(minSizeY, minRowHeight[row]);

                index += 1;
            }

            // converting here to our "pretend" scenario where columns have a limit defined
            var (wSepActual, hSepActual) = (Vector2i) (Separations * UIScale);
            var wSep = _limitDimension == Dimension.Column ? wSepActual : hSepActual;
            var hSep = _limitDimension == Dimension.Column ? hSepActual : wSepActual;
            var minWidth = AccumSizes(minColWidth, wSep);
            var minHeight = AccumSizes(minRowHeight, hSep);

            // converting back from our pretend scenario where columns are limited
            return new Vector2(
                _limitDimension == Dimension.Column ? minWidth : minHeight,
                _limitDimension == Dimension.Column ? minHeight : minWidth) / UIScale;
        }

        private static int AccumSizes(Span<int> sizes, int separator)
        {
            var totalSize = 0;
            var first = true;

            foreach (var size in sizes)
            {
                totalSize += size;

                if (first)
                {
                    first = false;
                }
                else
                {
                    totalSize += separator;
                }
            }

            return totalSize;
        }

        protected override Vector2 ArrangeOverride(Vector2 finalSize)
        {
               // to make it easier to read and visualize, we're just going to use the terms "x" and "y", width, and height,
            // rows and cols,
            // but at the start of the method here we'll set those to what they actually are based
            // on the limited dimension, which might involve swapping them.
            // For the below convention, we pretend that columns have a limit defined, thus
            // the amount of rows is not limited (unlimited).

            var rows = GetCount(UnlimitedDimension);
            var cols = GetCount(LimitedDimension);
            var cellSize = CellSize();

            Span<int> minColWidth = stackalloc int[cols];
            // Minimum lateral size of the unlimited dimension
            // (i.e. width of columns, height of rows).
            Span<int> minRowHeight = stackalloc int[rows];
            // columns that are set to expand vertically
            Span<bool> colExpand = stackalloc bool[cols];
            // rows that are set to expand horizontally
            Span<bool> rowExpand = stackalloc bool[rows];

            minColWidth.Fill(0);
            minRowHeight.Fill(0);
            colExpand.Fill(false);
            rowExpand.Fill(false);

            // Get minSize and size flag expand of each column and row.
            // All we need to apply the same logic BoxContainer does.
            var index = 0;
            foreach (var child in Children)
            {
                if (!child.Visible)
                {
                    continue;
                }

                var column = index % cols;
                var row = index / cols;

                // converting here to our "pretend" scenario where columns have a limit defined
                // note if we are limiting by size rather than count, the size of each child is constant (cell size)
                var (minSizeXActual, minSizeYActual) = _limitType == LimitType.Count ? child.DesiredPixelSize : cellSize;
                var minSizeX = _limitDimension == Dimension.Column ? minSizeXActual : minSizeYActual;
                var minSizeY = _limitDimension == Dimension.Column ? minSizeYActual : minSizeXActual;
                minColWidth[column] = Math.Max(minSizeX, minColWidth[column]);
                minRowHeight[row] = Math.Max(minSizeY, minRowHeight[row]);
                var colExpandFlag = _limitDimension == Dimension.Column
                    ? child.HorizontalExpand
                    : child.VerticalExpand;
                var rowExpandFlag = UnlimitedDimension == Dimension.Column
                    ? child.HorizontalExpand
                    : child.VerticalExpand;
                colExpand[column] = colExpand[column] || colExpandFlag;
                rowExpand[row] = rowExpand[row] || rowExpandFlag;

                index += 1;
            }

            // Basically now we just apply BoxContainer logic on rows and columns.
            var stretchMinX = 0;
            var stretchMinY = 0;
            // We do not use stretch ratios because Godot doesn't,
            // which makes sense since what happens if two things on the same column have a different stretch ratio?
            // Maybe there's an answer for that but I'm too lazy to think of a concrete solution
            // and it would make this code more complex so...
            // pass.
            var stretchCountX = 0;
            var stretchCountY = 0;

            for (var i = 0; i < minColWidth.Length; i++)
            {
                if (!colExpand[i])
                {
                    stretchMinX += minColWidth[i];
                }
                else
                {
                    stretchCountX++;
                }
            }

            for (var i = 0; i < minRowHeight.Length; i++)
            {
                if (!rowExpand[i])
                {
                    stretchMinY += minRowHeight[i];
                }
                else
                {
                    stretchCountY++;
                }
            }

            // converting here to our "pretend" scenario where columns have a limit defined
            var (vSepActual, hSepActual) = (Vector2i) (Separations * UIScale);
            var hSep = _limitDimension == Dimension.Column ? hSepActual : vSepActual;
            var vSep = _limitDimension == Dimension.Column ? vSepActual : hSepActual;
            var width = (_limitDimension == Dimension.Column ? finalSize.X : finalSize.Y) * UIScale;
            var height = (_limitDimension == Dimension.Column ? finalSize.Y : finalSize.X) * UIScale;

            var stretchMaxX = width - hSep * (cols - 1);
            var stretchMaxY = height - vSep * (rows - 1);

            var stretchAvailX = Math.Max(0, stretchMaxX - stretchMinX);
            var stretchAvailY = Math.Max(0, stretchMaxY - stretchMinY);

            for (var i = 0; i < minColWidth.Length; i++)
            {
                if (!colExpand[i])
                {
                    continue;
                }

                minColWidth[i] = (int) (stretchAvailX / stretchCountX);
            }

            for (var i = 0; i < minRowHeight.Length; i++)
            {
                if (!rowExpand[i])
                {
                    continue;
                }

                minRowHeight[i] = (int) (stretchAvailY / stretchCountY);
            }

            // Actually lay them out.
            // if inverted, (in our pretend "columns are limited" scenario) we must calculate the final
            // height (as height will vary depending on number of elements), and then
            // go backwards, starting from the bottom and filling elements in upwards
            var finalVOffset = 0;
            if (ExpandBackwards)
            {
                // we have to iterate through the elements first to determine the height each
                // row will end up having, as they can vary
                index = 0;
                for (var i = 0; i < ChildCount; i++, index++)
                {
                    var child = GetChild(i);
                    if (!child.Visible)
                    {
                        index--;
                        continue;
                    }

                    var column = index % cols;
                    var row = index / cols;

                    if (column == 0)
                    {
                        // Just started a new row/col.
                        if (row != 0)
                        {
                            finalVOffset += vSep + minRowHeight[row - 1];
                        }
                    }
                }
            }

            var hOffset = 0;
            var vOffset = ExpandBackwards ? finalVOffset : 0;
            index = 0;
            for (var i = 0; i < ChildCount; i++, index++)
            {
                var child = GetChild(i);
                if (!child.Visible)
                {
                    index--;
                    continue;
                }

                var column = index % cols;
                var row = index / cols;

                if (column == 0)
                {
                    // Just started a new row
                    hOffset = 0;
                    if (row != 0)
                    {
                        if (ExpandBackwards)
                        {
                            // every time we start a new row we actually decrease the voffset, we are filling
                            // in the up direction
                            vOffset -= vSep + minRowHeight[row - 1];
                        }
                        else
                        {
                            vOffset += vSep + minRowHeight[row - 1];
                        }

                    }
                }

                // converting back from our "pretend" scenario
                var left = _limitDimension == Dimension.Column ? hOffset : vOffset;
                var top = _limitDimension == Dimension.Column ? vOffset : hOffset;
                var boxWidth = _limitDimension == Dimension.Column ? minColWidth[column] : minRowHeight[row];
                var boxHeight = _limitDimension == Dimension.Column ? minRowHeight[row] : minColWidth[column];

                var box = UIBox2i.FromDimensions(left, top, boxWidth, boxHeight);
                child.ArrangePixel(box);

                hOffset += minColWidth[column] + hSep;
            }

            return finalSize;
        }
    }

    public enum Dimension : byte
    {
        Column,
        Row
    }

    public enum LimitType : byte
    {
        /// <summary>
        /// Defined number of rows or columns
        /// </summary>
        Count,
        /// <summary>
        /// Defined max width or height, inside of which the number of rows or columns
        /// will be fit.
        /// </summary>
        Size
    }
}
