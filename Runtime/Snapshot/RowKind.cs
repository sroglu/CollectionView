namespace PFound.CollectionView.Snapshot
{
    /// <summary>The three row kinds of the materialized snapshot's discriminated union.</summary>
    public enum RowKind
    {
        /// <summary>A section header row.</summary>
        SectionHeader = 0,

        /// <summary>A row hosting up to <c>columns</c> item members (the hidden grid trick).</summary>
        ItemRow = 1,

        /// <summary>An inline "this section is empty" placeholder row.</summary>
        SectionEmpty = 2
    }
}
