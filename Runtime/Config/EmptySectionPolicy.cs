namespace PFound.CollectionView.Config
{
    /// <summary>
    /// How a section with zero visible items is presented, distinguishing a section that is
    /// <em>structurally empty</em> (no items at all) from one that was <em>filtered to empty</em>
    /// (had items, the filter removed them).
    /// </summary>
    public enum EmptySectionPolicy
    {
        /// <summary>
        /// Default. Any section with 0 visible items is omitted entirely (no header).
        /// Best for search/filter UX.
        /// </summary>
        Hide = 0,

        /// <summary>
        /// Show an inline "no matches" placeholder for sections that HAD items but filtered to
        /// empty; still hide structurally-empty sections. Best for "taxonomy + search" screens.
        /// </summary>
        ShowIfFilteredEmpty = 1,

        /// <summary>
        /// Always show every section header plus an inline empty placeholder.
        /// Best for fixed-taxonomy dashboards.
        /// </summary>
        ShowPlaceholder = 2
    }
}
