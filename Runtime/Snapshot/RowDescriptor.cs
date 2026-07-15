using System;
using System.Collections.Generic;
using PFound.CollectionView.Items;

namespace PFound.CollectionView.Snapshot
{
    /// <summary>
    /// Immutable discriminated union describing one row of the materialized snapshot.
    /// A row is one of: <see cref="RowKind.SectionHeader"/>, <see cref="RowKind.ItemRow"/>
    /// (up to <c>columns</c> members), or <see cref="RowKind.SectionEmpty"/>. Every descriptor
    /// also carries its own <see cref="Height"/> (feature B: variable size) and
    /// <see cref="TemplateKey"/> (feature A: heterogeneous templates) so delegate queries are O(1).
    /// </summary>
    public readonly struct RowDescriptor
    {
        public readonly RowKind Kind;

        /// <summary>Section this row belongs to (header/empty), or the members' section (item row).</summary>
        public readonly object SectionKey;

        /// <summary>Header payload for a <see cref="RowKind.SectionHeader"/> row; otherwise null.</summary>
        public readonly object HeaderData;

        /// <summary>Members for a <see cref="RowKind.ItemRow"/>; empty array for header/empty rows.</summary>
        public readonly IReadOnlyList<ICollectionItem> Items;

        /// <summary>Row extent along the scroll axis (pixels).</summary>
        public readonly float Height;

        /// <summary>Template key used to pick the cell prefab that renders this row.</summary>
        public readonly string TemplateKey;

        RowDescriptor(RowKind kind, object sectionKey, object headerData, IReadOnlyList<ICollectionItem> items, float height, string templateKey)
        {
            Kind = kind;
            SectionKey = sectionKey;
            HeaderData = headerData;
            Items = items;
            Height = height;
            TemplateKey = templateKey;
        }

        public static RowDescriptor Header(object sectionKey, object headerData, float height, string templateKey)
            => new RowDescriptor(RowKind.SectionHeader, sectionKey, headerData, Array.Empty<ICollectionItem>(), height, templateKey);

        public static RowDescriptor Row(object sectionKey, IReadOnlyList<ICollectionItem> items, float height, string templateKey)
            => new RowDescriptor(RowKind.ItemRow, sectionKey, null, items, height, templateKey);

        public static RowDescriptor Empty(object sectionKey, float height, string templateKey)
            => new RowDescriptor(RowKind.SectionEmpty, sectionKey, null, Array.Empty<ICollectionItem>(), height, templateKey);
    }
}
