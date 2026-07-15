using System;
using System.Collections.Generic;
using PFound.CollectionView.Config;
using PFound.CollectionView.Items;

namespace PFound.CollectionView.Snapshot
{
    /// <summary>
    /// Layout / policy inputs for <see cref="SnapshotFlattener"/>. Fixed heights + template keys give the
    /// fast path; the optional resolvers enable feature A (heterogeneous templates) and feature B
    /// (variable / self-measured cell size) purely by returning a per-row value.
    /// </summary>
    public sealed class FlattenSettings
    {
        /// <summary>Grouping on/off. Off -> one implicit section, no headers, no inline empty rows (flat mode).</summary>
        public bool Grouping;

        /// <summary>Grid width: how many item members a single <see cref="RowKind.ItemRow"/> hosts. Must be >= 1.</summary>
        public int Columns = 1;

        /// <summary>Empty-section presentation policy (grouped mode only).</summary>
        public EmptySectionPolicy EmptyPolicy = EmptySectionPolicy.Hide;

        /// <summary>Fixed row heights (fast path). Overridden per row by the matching resolver when set.</summary>
        public float HeaderHeight = 1f;
        public float ItemRowHeight = 1f;
        public float EmptyRowHeight = 1f;

        /// <summary>Fixed template keys. Overridden per item row by <see cref="ItemRowTemplateResolver"/> when set.</summary>
        public string HeaderTemplateKey = CollectionTemplateKeys.Header;
        public string ItemRowTemplateKey = CollectionTemplateKeys.ItemRow;
        public string EmptyTemplateKey = CollectionTemplateKeys.Empty;

        /// <summary>Feature B: per-item-row height. Null -> use <see cref="ItemRowHeight"/>.</summary>
        public Func<IReadOnlyList<ICollectionItem>, float> ItemRowHeightResolver;

        /// <summary>Feature A: per-item-row template key. Null -> use <see cref="ItemRowTemplateKey"/>.</summary>
        public Func<IReadOnlyList<ICollectionItem>, string> ItemRowTemplateResolver;

        internal float ResolveItemRowHeight(IReadOnlyList<ICollectionItem> row)
            => ItemRowHeightResolver == null ? ItemRowHeight : ItemRowHeightResolver(row);

        internal string ResolveItemRowTemplate(IReadOnlyList<ICollectionItem> row)
            => ItemRowTemplateResolver == null ? ItemRowTemplateKey : ItemRowTemplateResolver(row);
    }
}
