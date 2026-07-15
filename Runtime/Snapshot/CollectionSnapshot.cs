using System.Collections.Generic;

namespace PFound.CollectionView.Snapshot
{
    /// <summary>
    /// Immutable, materialized flat view the scroller delegate indexes into. Produced once per
    /// structural change by <see cref="SnapshotFlattener"/>. Every delegate query is O(1) array
    /// indexing - no running-offset index math anywhere downstream.
    /// </summary>
    public sealed class CollectionSnapshot
    {
        /// <summary>The flat row list, in display order.</summary>
        public IReadOnlyList<RowDescriptor> Rows { get; }

        /// <summary>Row index of the row that hosts a given item identity, for scroll-to-item.</summary>
        public IReadOnlyDictionary<object, int> IndexOf { get; }

        /// <summary>Total number of visible item members across all sections (headers excluded).</summary>
        public int VisibleItemCount { get; }

        /// <summary>True when no item is visible (all sections empty/hidden). Drives the global empty state.</summary>
        public bool IsEmpty => VisibleItemCount == 0;

        public CollectionSnapshot(IReadOnlyList<RowDescriptor> rows, IReadOnlyDictionary<object, int> indexOf, int visibleItemCount)
        {
            Rows = rows;
            IndexOf = indexOf;
            VisibleItemCount = visibleItemCount;
        }

        static readonly CollectionSnapshot _empty = new CollectionSnapshot(
            new RowDescriptor[0], new Dictionary<object, int>(), 0);

        /// <summary>A shared empty snapshot (zero rows, zero items).</summary>
        public static CollectionSnapshot Empty => _empty;
    }
}

