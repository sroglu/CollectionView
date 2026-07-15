namespace PFound.CollectionView.Items
{
    /// <summary>
    /// Optional role placing an item into a group/section. Absent -> the item lives in the
    /// single implicit section (flat mode). Present -> the item is partitioned by
    /// <see cref="SectionKey"/> and, within its section, may carry an explicit intra-section order.
    /// </summary>
    public interface IGroupable
    {
        /// <summary>Opaque key identifying the section this item belongs to.</summary>
        object SectionKey { get; }

        /// <summary>
        /// Optional stable intra-section order hint. Only consulted when the host registers
        /// the built-in intra-section-order sort key; otherwise ignored in favour of the
        /// active composite sort. Lower values sort earlier.
        /// </summary>
        int IntraSectionOrder { get; }
    }
}
