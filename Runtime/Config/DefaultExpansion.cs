namespace PFound.CollectionView.Config
{
    /// <summary>Initial expansion state applied to sections when a grouped view first builds.</summary>
    public enum DefaultExpansion
    {
        /// <summary>Every section starts expanded.</summary>
        AllExpanded = 0,

        /// <summary>Every section starts collapsed (headers only).</summary>
        AllCollapsed = 1,

        /// <summary>Expansion is decided per section by the host (no blanket default applied).</summary>
        PerSection = 2
    }
}
