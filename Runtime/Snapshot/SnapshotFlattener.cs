using System;
using System.Collections.Generic;
using System.Linq;
using PFound.CollectionView.Config;
using PFound.CollectionView.Filtering;
using PFound.CollectionView.Items;

namespace PFound.CollectionView.Snapshot
{
    /// <summary>
    /// Bundle of inputs for a single <see cref="SnapshotFlattener.Flatten{T}"/> pass. Grouping the
    /// arguments keeps the flatten call a single readable statement.
    /// </summary>
    public struct FlattenInputs<T> where T : ICollectionItem
    {
        public IReadOnlyList<T> Items;
        public FlattenSettings Settings;
        public IExpansionQuery Expansion;
        public FilterPipeline<T> Filter;

        /// <summary>Intra-section composite comparer (Sec 5). Applied only when <see cref="Sorted"/> is true.</summary>
        public IComparer<T> ItemComparer;

        /// <summary>Whether an intra-section sort is active. False -> members keep source order.</summary>
        public bool Sorted;

        /// <summary>Grouped mode: item -> section key. Ignored in flat mode.</summary>
        public Func<T, object> SectionKeySelector;

        /// <summary>Grouped mode: section key -> host header payload (label/icon keys). Returns null for none.</summary>
        public Func<object, object> SectionHeaderProvider;

        /// <summary>Grouped mode: orders the sections themselves (Sec 11e).</summary>
        public Sorting.ISectionComparer SectionComparer;

        /// <summary>
        /// Optional fixed taxonomy (grouped mode): section keys that must exist even with zero items.
        /// Pre-seeds empty buckets so <see cref="Config.EmptySectionPolicy"/> can distinguish a
        /// <em>structurally empty</em> section (declared, no items) from a <em>filtered-to-empty</em> one.
        /// Null -> sections are derived purely from the items present.
        /// </summary>
        public IReadOnlyList<object> DeclaredSectionKeys;
    }

    /// <summary>
    /// The unifying core: turns items + policy into an immutable <see cref="CollectionSnapshot"/> in a
    /// single readable linear pass, with NO running-offset index math. Flat lists and grouped lists are
    /// the SAME code path - a flat list is just "one implicit section, zero headers".
    /// See design brief Sec 11b (flatten) and Sec 11e (grouping/ordering/filtering composition).
    /// </summary>
    public static class SnapshotFlattener
    {
        static readonly object FlatSectionKey = new object();

        public static CollectionSnapshot Flatten<T>(FlattenInputs<T> input) where T : ICollectionItem
        {
            var settings = input.Settings;
            var rows = new List<RowDescriptor>();
            var indexOf = new Dictionary<object, int>();
            int visibleItemTotal = 0;

            if (!settings.Grouping)
            {
                // Flat mode: one implicit section, no header, no inline empty row.
                var members = ProcessSection(input, AllItems(input.Items));
                visibleItemTotal += members.Count;
                EmitItemRows(members, FlatSectionKey, settings, rows, indexOf);
                return new CollectionSnapshot(rows, indexOf, visibleItemTotal);
            }

            // ---- Grouped mode (Sec 11e) ----
            // 1. partition items into sections by section key (first-encounter order preserved).
            var buckets = Partition(input);

            // 2. per section: filter -> sort -> keep counts.
            var processed = new List<ProcessedSection<T>>(buckets.Count);
            for (int i = 0; i < buckets.Count; i++)
            {
                var bucket = buckets[i];
                var visible = ProcessSection(input, bucket.Members);
                processed.Add(new ProcessedSection<T>(bucket.Key, visible, bucket.Members.Count));
            }

            // 3. order the sections via the section comparer (stable).
            processed = StableOrderSections(processed, input.SectionComparer);

            // 4. emit per ordered section: header -> (skip if collapsed) -> item rows | empty.
            for (int i = 0; i < processed.Count; i++)
            {
                var section = processed[i];
                bool hadItems = section.TotalCount > 0;
                bool visibleEmpty = section.VisibleMembers.Count == 0;

                if (visibleEmpty && !ShouldShowEmptySection(settings.EmptyPolicy, hadItems))
                {
                    continue; // section omitted entirely (no header)
                }

                bool collapsed = input.Expansion.IsCollapsed(section.Key);
                object hostData = input.SectionHeaderProvider(section.Key);
                var headerData = new SectionHeaderData(section.Key, hostData, section.VisibleMembers.Count, section.TotalCount, collapsed);
                rows.Add(RowDescriptor.Header(section.Key, headerData, settings.HeaderHeight, settings.HeaderTemplateKey));

                if (collapsed)
                {
                    continue; // members excluded -> drop out of the row count
                }

                if (visibleEmpty)
                {
                    rows.Add(RowDescriptor.Empty(section.Key, settings.EmptyRowHeight, settings.EmptyTemplateKey));
                    continue;
                }

                visibleItemTotal += section.VisibleMembers.Count;
                EmitItemRows(section.VisibleMembers, section.Key, settings, rows, indexOf);
            }

            return new CollectionSnapshot(rows, indexOf, visibleItemTotal);
        }

        static List<T> AllItems<T>(IReadOnlyList<T> items)
        {
            var list = new List<T>(items.Count);
            for (int i = 0; i < items.Count; i++)
            {
                list.Add(items[i]);
            }
            return list;
        }

        static List<T> ProcessSection<T>(FlattenInputs<T> input, List<T> members) where T : ICollectionItem
        {
            var visible = new List<T>(members.Count);
            for (int i = 0; i < members.Count; i++)
            {
                if (input.Filter.Matches(members[i]))
                {
                    visible.Add(members[i]);
                }
            }

            if (input.Sorted && visible.Count > 1)
            {
                // Stable sort so tie-broken/equal members keep source order deterministically.
                return visible.OrderBy(x => x, input.ItemComparer).ToList();
            }
            return visible;
        }

        static void EmitItemRows<T>(List<T> members, object sectionKey, FlattenSettings settings,
            List<RowDescriptor> rows, Dictionary<object, int> indexOf) where T : ICollectionItem
        {
            int columns = settings.Columns < 1 ? 1 : settings.Columns;
            for (int start = 0; start < members.Count; start += columns)
            {
                int end = Math.Min(start + columns, members.Count);
                var rowItems = new ICollectionItem[end - start];
                for (int j = start; j < end; j++)
                {
                    rowItems[j - start] = members[j];
                }

                int rowIndex = rows.Count;
                float height = settings.ResolveItemRowHeight(rowItems);
                string template = settings.ResolveItemRowTemplate(rowItems);
                rows.Add(RowDescriptor.Row(sectionKey, rowItems, height, template));

                for (int j = 0; j < rowItems.Length; j++)
                {
                    indexOf[rowItems[j].IdentityKey] = rowIndex;
                }
            }
        }

        static List<Bucket<T>> Partition<T>(FlattenInputs<T> input) where T : ICollectionItem
        {
            var byKey = new Dictionary<object, int>();
            var buckets = new List<Bucket<T>>();

            // Seed declared taxonomy first (grouped fixed-taxonomy dashboards): these may end up empty.
            var declared = input.DeclaredSectionKeys;
            if (declared != null)
            {
                for (int i = 0; i < declared.Count; i++)
                {
                    object key = declared[i];
                    if (!byKey.ContainsKey(key))
                    {
                        byKey[key] = buckets.Count;
                        buckets.Add(new Bucket<T>(key));
                    }
                }
            }

            var items = input.Items;
            for (int i = 0; i < items.Count; i++)
            {
                object key = input.SectionKeySelector(items[i]);
                if (!byKey.TryGetValue(key, out int idx))
                {
                    idx = buckets.Count;
                    byKey[key] = idx;
                    buckets.Add(new Bucket<T>(key));
                }
                buckets[idx].Members.Add(items[i]);
            }
            return buckets;
        }

        static List<ProcessedSection<T>> StableOrderSections<T>(List<ProcessedSection<T>> sections, Sorting.ISectionComparer comparer)
        {
            return sections
                .OrderBy(s => new Sorting.SectionView(s.Key, null, s.VisibleMembers.Count), comparer)
                .ToList();
        }

        static bool ShouldShowEmptySection(EmptySectionPolicy policy, bool hadItems)
        {
            switch (policy)
            {
                case EmptySectionPolicy.Hide:
                    return false;
                case EmptySectionPolicy.ShowIfFilteredEmpty:
                    return hadItems; // had items, filtered to empty -> show; structurally empty -> hide
                case EmptySectionPolicy.ShowPlaceholder:
                    return true;
                default:
                    return false;
            }
        }

        sealed class Bucket<T>
        {
            public readonly object Key;
            public readonly List<T> Members = new List<T>();
            public Bucket(object key) { Key = key; }
        }

        readonly struct ProcessedSection<T>
        {
            public readonly object Key;
            public readonly List<T> VisibleMembers;
            public readonly int TotalCount;

            public ProcessedSection(object key, List<T> visibleMembers, int totalCount)
            {
                Key = key;
                VisibleMembers = visibleMembers;
                TotalCount = totalCount;
            }
        }
    }
}
