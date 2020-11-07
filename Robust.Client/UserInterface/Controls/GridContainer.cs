using System;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    /// <summary>
    ///     A container that lays out its children in a grid. Can define specific amount of
    ///     rows or specific amount of columns (not both), and will grow to fill in additional rows/columns within
    ///     that limit.
    /// </summary>
    public class GridContainer : Container
    {
        // limit - depending on mode, this is either rows or columns
        private int _limitedDimensionAmount = 1;

        private Dimension _limitDimension = Dimension.Column;

        /// <summary>
        /// Indicates whether row or column amount has been specified, and thus
        /// how items will fill them out as they are added.
        /// This is set depending on whether you have specified Columns or Rows.
        /// </summary>
        public Dimension LimitedDimension => _limitDimension;
        /// <summary>
        /// Opposite dimension of LimitedDimension
        /// </summary>
        public Dimension UnlimitedDimension => _limitDimension == Dimension.Column ? Dimension.Row : Dimension.Column;

        /// <summary>
        ///     The amount of columns to organize the children into. Setting this puts this grid
        ///     into LimitMode.LimitColumns - items will be added to fill up the entire row, up to the defined
        ///     limit of columns, and then create a second row.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown if the value assigned is less than or equal to 0.
        /// </exception>
        /// <returns>specified limit if LimitMode.LimitColums, otherwise the amount
        /// of columns being used for the current amount of children.</returns>
        public int Columns
        {
            get => GetAmount(Dimension.Column);
            set => SetAmount(Dimension.Column, value);
        }

        /// <summary>
        ///     The amount of rows to organize the children into. Setting this puts this grid
        ///     into LimitMode.LimitRows - items will be added to fill up the entire column, up to the defined
        ///     limit of rows, and then create a second column.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown if the value assigned is less than or equal to 0.
        /// </exception>
        /// <returns>specified limit if LimitMode.LimitRows, otherwise the amount
        /// of rows being used for the current amount of children.</returns>
        public int Rows
        {
            get => GetAmount(Dimension.Row);
            set => SetAmount(Dimension.Row, value);
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

        private int GetAmount(Dimension forDimension)
        {
            if (forDimension == _limitDimension) return _limitedDimensionAmount;
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

        private void SetAmount(Dimension forDimension, int value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Value must be greater than zero.");
            }

            _limitDimension = forDimension;

            _limitedDimensionAmount = value;
            MinimumSizeChanged();
            UpdateLayout();
        }

        protected override Vector2 CalculateMinimumSize()
        {
            var unlimitedDimensionAmount = GetAmount(UnlimitedDimension);

            // Minimum lateral size of the limited dimension
            // (i.e. width of columns, height of rows).
            Span<int> limitedSize = stackalloc int[_limitedDimensionAmount];
            // Minimum lateral size of the unlimited dimension
            // (i.e. width of columns, height of rows).
            Span<int> unlimitedSize = stackalloc int[unlimitedDimensionAmount];

            var index = 0;
            foreach (var child in Children)
            {
                if (!child.Visible)
                {
                    index--;
                    continue;
                }

                var limitedIdx = index % _limitedDimensionAmount;
                var unlimitedIdx = index / _limitedDimensionAmount;


                var (minSizeX, minSizeY) = child.CombinedPixelMinimumSize;
                var minSizeLimited = _limitDimension == Dimension.Row ? minSizeY : minSizeX;
                var minSizeUnlimited = UnlimitedDimension == Dimension.Row ? minSizeY : minSizeX;
                limitedSize[limitedIdx] = Math.Max(minSizeLimited, limitedSize[limitedIdx]);
                unlimitedSize[unlimitedIdx] = Math.Max(minSizeUnlimited, unlimitedSize[unlimitedIdx]);

                index += 1;
            }

            var (wSep, hSep) = (Vector2i) (Separations * UIScale);
            var limitedDimensionSep = _limitDimension == Dimension.Row ? hSep : wSep;
            var unlimitedDimensionSep = UnlimitedDimension == Dimension.Row ? hSep : wSep;
            var minLimitedDimension = AccumSizes(limitedSize, limitedDimensionSep);
            var minUnlimitedDimension = AccumSizes(unlimitedSize, unlimitedDimensionSep);

            // the min of columns is width, the min of rows is height,
            // so if we limited columns, the minLimitedDimension is our width, i.e. x coord
            return new Vector2(
                _limitDimension == Dimension.Column ? minLimitedDimension : minUnlimitedDimension,
                _limitDimension == Dimension.Column ? minUnlimitedDimension : minLimitedDimension) / UIScale;
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

        protected override void LayoutUpdateOverride()
        {
            var unlimitedDimensionAmount = GetAmount(UnlimitedDimension);

            // Minimum lateral size of the limited dimension
            // (i.e. width of columns, height of rows).
            Span<int> limitedSize = stackalloc int[_limitedDimensionAmount];
            // Minimum lateral size of the unlimited dimension
            // (i.e. width of columns, height of rows).
            Span<int> unlimitedSize = stackalloc int[unlimitedDimensionAmount];
            // elements of the limited dimension that are set to expand laterally
            // (i.e. if limited dimension is column, this is indicating which column
            // is set to expand horizontally)
            Span<bool> limitedExpandSize = stackalloc bool[_limitedDimensionAmount];
            // elements of the unlimited dimension that are set to expand laterally
            Span<bool> unlimitedExpandSize = stackalloc bool[unlimitedDimensionAmount];

            // Get minSize and size flag expand of each column and row.
            // All we need to apply the same logic BoxContainer does.
            var index = 0;
            foreach (var child in Children)
            {
                if (!child.Visible)
                {
                    continue;
                }

                var limitedIdx = index % _limitedDimensionAmount;
                var unlimitedIdx = index / _limitedDimensionAmount;

                var (minSizeX, minSizeY) = child.CombinedPixelMinimumSize;
                var minSizeLimited = _limitDimension == Dimension.Column ? minSizeX : minSizeY;
                var minSizeUnlimited = UnlimitedDimension == Dimension.Column ? minSizeX : minSizeY;
                limitedSize[limitedIdx] = Math.Max(minSizeLimited, limitedSize[limitedIdx]);
                unlimitedSize[unlimitedIdx] = Math.Max(minSizeUnlimited, unlimitedSize[unlimitedIdx]);
                var limitedSizeFlag = _limitDimension == Dimension.Column
                    ? child.SizeFlagsHorizontal
                    : child.SizeFlagsVertical;
                var unlimitedSizeFlag = UnlimitedDimension == Dimension.Column
                    ? child.SizeFlagsHorizontal
                    : child.SizeFlagsVertical;
                limitedExpandSize[limitedIdx] = limitedExpandSize[limitedIdx] || (limitedSizeFlag & SizeFlags.Expand) != 0;
                unlimitedExpandSize[unlimitedIdx] = unlimitedExpandSize[unlimitedIdx] || (unlimitedSizeFlag & SizeFlags.Expand) != 0;

                index += 1;
            }

            // Basically now we just apply BoxContainer logic on rows and columns.
            var stretchMinLimited = 0;
            var stretchMinUnlimited = 0;
            // We do not use stretch ratios because Godot doesn't,
            // which makes sense since what happens if two things on the same column have a different stretch ratio?
            // Maybe there's an answer for that but I'm too lazy to think of a concrete solution
            // and it would make this code more complex so...
            // pass.
            var stretchCountLimited = 0;
            var stretchCountUnlimited = 0;

            for (var i = 0; i < limitedSize.Length; i++)
            {
                if (!limitedExpandSize[i])
                {
                    stretchMinLimited += limitedSize[i];
                }
                else
                {
                    stretchCountLimited++;
                }
            }

            for (var i = 0; i < unlimitedSize.Length; i++)
            {
                if (!unlimitedExpandSize[i])
                {
                    stretchMinUnlimited += unlimitedSize[i];
                }
                else
                {
                    stretchCountUnlimited++;
                }
            }

            var (vSep, hSep) = (Vector2i) (Separations * UIScale);
            var limitedDimensionSep = _limitDimension == Dimension.Column ? hSep : vSep;
            var unlimitedDimensionSep = UnlimitedDimension == Dimension.Column ? hSep : vSep;
            var limitedDimSize = _limitDimension == Dimension.Column ? Width : Height;
            var unlimitedDimSize = UnlimitedDimension == Dimension.Column ? Width : Height;

            var stretchMaxLimited = limitedDimSize - limitedDimensionSep * (_limitedDimensionAmount - 1);
            var stretchMaxUnlimited = unlimitedDimSize - unlimitedDimensionSep * (unlimitedDimensionAmount - 1);

            var stretchAvailLimited = Math.Max(0, stretchMaxLimited - stretchMinLimited);
            var stretchAvailUnlimited = Math.Max(0, stretchMaxUnlimited - stretchMinUnlimited);

            for (var i = 0; i < limitedSize.Length; i++)
            {
                if (!limitedExpandSize[i])
                {
                    continue;
                }

                limitedSize[i] = (int) (stretchAvailLimited / stretchCountLimited);
            }

            for (var i = 0; i < unlimitedSize.Length; i++)
            {
                if (!unlimitedExpandSize[i])
                {
                    continue;
                }

                unlimitedSize[i] = (int) (stretchAvailUnlimited / stretchCountUnlimited);
            }

            // Actually lay them out.
            var limitedOffset = 0;
            var unlimitedOffset = 0;
            index = 0;
            for (var i = 0; i < ChildCount; i++, index++)
            {
                var child = GetChild(i);
                if (!child.Visible)
                {
                    index--;
                    continue;
                }

                var limitedIdx = index % _limitedDimensionAmount;
                var unlimitedIdx = index / _limitedDimensionAmount;

                if (limitedIdx == 0)
                {
                    // Just started a new row/col.
                    limitedOffset = 0;
                    if (unlimitedIdx != 0)
                    {
                        unlimitedOffset += unlimitedDimensionSep + unlimitedSize[unlimitedIdx - 1];
                    }
                }

                var left = _limitDimension == Dimension.Column ? limitedOffset : unlimitedOffset;
                var top = UnlimitedDimension == Dimension.Column ? limitedOffset : unlimitedOffset;
                var width = _limitDimension == Dimension.Column ? limitedSize[limitedIdx] : unlimitedSize[unlimitedIdx];
                var height = UnlimitedDimension == Dimension.Column ? limitedSize[limitedIdx] : unlimitedSize[unlimitedIdx];

                var box = UIBox2i.FromDimensions(left, top, width, height);
                FitChildInPixelBox(child, box);

                limitedOffset += limitedSize[limitedIdx] + limitedDimensionSep;
            }
        }
    }

    public enum Dimension
    {
        Column,
        Row
    }
}
