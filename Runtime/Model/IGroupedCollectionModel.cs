using System;
using PFound.CollectionView.Snapshot;

namespace PFound.CollectionView.Model
{
    /// <summary>
    /// The canonical, view-independent data/ordering service. Owns the source set, the current sort
    /// selection, the filter pipeline, and (grouped mode) sections + expansion. The ONLY thing it
    /// produces for the view is an immutable <see cref="CollectionSnapshot"/>. Fully unit-testable -
    /// no scene, no scroller, no Unity types beyond plain data.
    /// </summary>
    public interface IGroupedCollectionModel<T> where T : PFound.CollectionView.Items.ICollectionItem
    {
        /// <summary>Raised whenever the visible list changes (source, filter, sort, or expansion mutation).</summary>
        event Action Changed;

        /// <summary>Filtered + ordered visible item count (item space, not row space).</summary>
        int Count { get; }

        /// <summary>The item at a filtered+ordered visible index (item space).</summary>
        T ItemAt(int index);

        /// <summary>Filter-aware visible index of an identity, or -1 if not visible (item space).</summary>
        int IndexOf(object identityKey);

        /// <summary>Materializes the current immutable snapshot (row space) the view renders.</summary>
        CollectionSnapshot BuildSnapshot();
    }
}
