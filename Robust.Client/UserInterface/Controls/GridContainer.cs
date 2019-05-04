using System;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    [ControlWrap("GridContainer")]
    public class GridContainer : Container
    {
        private int _columns = 1;

        public GridContainer()
        {
        }

        public GridContainer(string name) : base(name)
        {
        }

        public int Columns
        {
            get => _columns;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Value must be greater than zero.");
                }

                _columns = value;
                MinimumSizeChanged();
                SortChildren();
            }
        }

        public int Rows
        {
            get
            {
                if (ChildCount == 0)
                {
                    return 1;
                }

                var div = ChildCount / Columns;
                if (ChildCount % Columns != 0)
                {
                    div += 1;
                }

                return div;
            }
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

        private (int h, int v) Separations => (_hSeparationOverride ?? 4, _vSeparationOverride ?? 4);

        protected override Vector2 CalculateMinimumSize()
        {
            var firstRow = true;
            var totalMinSize = Vector2.Zero;
            var thisRowSize = Vector2.Zero;
            var currentRowCount = 0;
            var (h, v) = Separations;

            foreach (var child in Children)
            {
                if (!child.Visible)
                {
                    continue;
                }

                var (minSizeX, minSizeY) = child.CombinedMinimumSize;
                thisRowSize = (thisRowSize.X + minSizeX, Math.Max(thisRowSize.Y, minSizeY));
                if (currentRowCount != 0)
                {
                    thisRowSize += (h, 0);
                }

                if (++currentRowCount == _columns)
                {
                    totalMinSize = (Math.Max(thisRowSize.X, totalMinSize.X), totalMinSize.Y + thisRowSize.Y);
                    if (!firstRow)
                    {
                        totalMinSize += (0, v);
                    }
                    firstRow = false;

                    thisRowSize = Vector2.Zero;
                    currentRowCount = 0;
                }
            }

            if (currentRowCount != 0)
            {
                totalMinSize = (Math.Max(thisRowSize.X, totalMinSize.X), totalMinSize.Y + thisRowSize.Y);
                if (!firstRow)
                {
                    totalMinSize += (0, v);
                }
            }

            return totalMinSize;
        }

        protected override void SortChildren()
        {
            var rows = Rows;

            // Minimum width of the columns.
            var columnSizes = new int[_columns];
            // Minimum height of the columns.
            var rowSizes = new int[rows];
            // Columns that are set to expand horizontally.
            var columnExpand = new bool[_columns];
            // Columns that are set to expand vertically.
            var rowExpand = new bool[rows];

            // Get minSize and size flag expand of each column and row.
            // All we need to apply the same logic BoxContainer does.
            var index = 0;
            for (var i = 0; i < ChildCount; i++, index++)
            {
                var child = GetChild(i);
                if (!child.Visible)
                {
                    index--;
                    continue;
                }

                var row = index / _columns;
                var column = index % _columns;

                var (minSizeX, minSizeY) = child.CombinedMinimumSize;
                columnSizes[column] = Math.Max((int) minSizeX, columnSizes[column]);
                rowSizes[row] = Math.Max((int) minSizeY, rowSizes[row]);
                columnExpand[column] = columnExpand[column] || (child.SizeFlagsHorizontal & SizeFlags.Expand) != 0;
                rowExpand[row] = rowExpand[row] || (child.SizeFlagsVertical & SizeFlags.Expand) != 0;
            }

            // Basically now we just apply BoxContainer logic on rows and columns.
            var (vSep, hSep) = Separations;
            var stretchMinX = 0;
            var stretchMinY = 0;
            // We do not use stretch ratios because Godot doesn't,
            // which makes sense since what happens if two things on the same column have a different stretch ratio?
            // Maybe there's an answer for that but I'm too lazy to think of a concrete solution
            // and it would make this code more complex so...
            // pass.
            var stretchCountX = 0;
            var stretchCountY = 0;

            for (var i = 0; i < columnSizes.Length; i++)
            {
                if (!columnExpand[i])
                {
                    stretchMinX += columnSizes[i];
                }
                else
                {
                    stretchCountX++;
                }
            }

            for (var i = 0; i < rowSizes.Length; i++)
            {
                if (!rowExpand[i])
                {
                    stretchMinY += rowSizes[i];
                }
                else
                {
                    stretchCountY++;
                }
            }

            var stretchMaxX = Size.X - hSep * (_columns - 1);
            var stretchMaxY = Size.Y - vSep * (rows - 1);

            var stretchAvailX = Math.Max(0, stretchMaxX - stretchMinX);
            var stretchAvailY = Math.Max(0, stretchMaxY - stretchMinY);

            for (var i = 0; i < columnSizes.Length; i++)
            {
                if (!columnExpand[i])
                {
                    continue;
                }

                columnSizes[i] = (int) (stretchAvailX / stretchCountX);
            }

            for (var i = 0; i < rowSizes.Length; i++)
            {
                if (!rowExpand[i])
                {
                    continue;
                }

                rowSizes[i] = (int) (stretchAvailY / stretchCountY);
            }

            // Actually lay them out.
            var vOffset = 0;
            var hOffset = 0;
            index = 0;
            for (var i = 0; i < ChildCount; i++, index++)
            {
                var child = GetChild(i);
                if (!child.Visible)
                {
                    index--;
                    continue;
                }

                var row = index / _columns;
                var column = index % _columns;

                if (column == 0)
                {
                    // Just started a new row.
                    hOffset = 0;
                    if (row != 0)
                    {
                        vOffset += vSep + rowSizes[row - 1];
                    }
                }

                var box = UIBox2.FromDimensions(hOffset, vOffset, columnSizes[column], rowSizes[row]);
                FitChildInBox(child, box);

                hOffset += columnSizes[column] + hSep;
            }
        }
    }
}
