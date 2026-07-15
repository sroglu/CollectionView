namespace PFound.CollectionView.View
{
    /// <summary>Result of a responsive column computation.</summary>
    public readonly struct GridLayout
    {
        /// <summary>Number of item members per row.</summary>
        public readonly int Columns;

        /// <summary>Actual width available per item cell after spacing.</summary>
        public readonly float CellWidth;

        /// <summary>Scale factor to apply to a design-sized cell so it fits the computed cell width.</summary>
        public readonly float Scale;

        public GridLayout(int columns, float cellWidth, float scale)
        {
            Columns = columns;
            CellWidth = cellWidth;
            Scale = scale;
        }
    }

    /// <summary>
    /// Pure responsive-grid math: derives items-per-row from container width, a per-item minimum width,
    /// and spacing, then a uniform scale so design-sized cells fit the computed cell width. No Unity types.
    /// </summary>
    public static class GridLayoutMath
    {
        /// <summary>
        /// Computes columns and per-cell width. <paramref name="designItemWidth"/> is the authored cell
        /// width used to derive the fit scale; pass the same value as <paramref name="minItemWidth"/> if
        /// cells should never scale below their minimum.
        /// </summary>
        public static GridLayout Compute(float containerWidth, float minItemWidth, float spacing, float designItemWidth)
        {
            float slot = minItemWidth + spacing;
            int columns = slot > 0f ? (int)((containerWidth + spacing) / slot) : 1;
            if (columns < 1)
            {
                columns = 1;
            }

            float cellWidth = (containerWidth - spacing * (columns - 1)) / columns;
            float scale = designItemWidth > 0f ? cellWidth / designItemWidth : 1f;
            return new GridLayout(columns, cellWidth, scale);
        }
    }
}
