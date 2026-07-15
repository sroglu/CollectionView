namespace PFound.CollectionView.Snapshot
{
    /// <summary>
    /// Header payload materialized during flatten. Wraps the host-supplied per-section data with the
    /// computed rollup (post-filter visible count + pre-filter total), so a header cell can render a
    /// "visible" or "visible / total" badge without recomputing anything. See Sec 11c / Sec 11e.
    /// </summary>
    public readonly struct SectionHeaderData
    {
        /// <summary>The section's opaque key.</summary>
        public readonly object SectionKey;

        /// <summary>Host-supplied display payload for the section (label key, icon key, ...). May be null.</summary>
        public readonly object HostData;

        /// <summary>Members visible after filtering.</summary>
        public readonly int VisibleCount;

        /// <summary>Members before filtering.</summary>
        public readonly int TotalCount;

        /// <summary>Whether the section is currently collapsed (members omitted from the snapshot).</summary>
        public readonly bool IsCollapsed;

        public SectionHeaderData(object sectionKey, object hostData, int visibleCount, int totalCount, bool isCollapsed)
        {
            SectionKey = sectionKey;
            HostData = hostData;
            VisibleCount = visibleCount;
            TotalCount = totalCount;
            IsCollapsed = isCollapsed;
        }
    }
}
