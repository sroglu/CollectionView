using System.Collections.Generic;
using NUnit.Framework;
using PFound.CollectionView.Filtering;
using PFound.CollectionView.Model;
using PFound.CollectionView.Snapshot;
using PFound.CollectionView.Sorting;

namespace PFound.CollectionView.Tests
{
    /// <summary>
    /// Sec 11e: when grouping is ON there are two independent ordering axes - section-level order
    /// (<see cref="ISectionComparer"/>) and intra-section item order (composite <see cref="ISortKey{T}"/>).
    /// Item sort never crosses section boundaries; filtering applies inside each section.
    /// </summary>
    public sealed class OrderingCompositionTests
    {
        static ISortKey<FakeItem> PriorityKey() => new DelegateSortKey<FakeItem>("priority", x => x.Priority);

        static FlattenInputs<FakeItem> Inputs(IReadOnlyList<FakeItem> items, ISectionComparer sectionComparer,
            IComparer<FakeItem> itemComparer, FilterPipeline<FakeItem> filter)
        {
            return new FlattenInputs<FakeItem>
            {
                Items = items,
                Settings = new FlattenSettings { Grouping = true, Columns = 1 },
                Expansion = new ExpansionState(),
                Filter = filter,
                ItemComparer = itemComparer,
                Sorted = true,
                SectionKeySelector = x => x.Section,
                SectionHeaderProvider = _ => null,
                SectionComparer = sectionComparer
            };
        }

        static List<string> ItemIdsInOrder(CollectionSnapshot snap)
        {
            var ids = new List<string>();
            for (int i = 0; i < snap.Rows.Count; i++)
            {
                if (snap.Rows[i].Kind == RowKind.ItemRow)
                {
                    ids.Add((string)snap.Rows[i].Items[0].IdentityKey);
                }
            }
            return ids;
        }

        [Test]
        public void SectionsOrderedByComparer_ItemsSortedWithinEachSection()
        {
            // Input order deliberately scrambled across sections and priorities.
            var items = new List<FakeItem>
            {
                new FakeItem("b_hi", section: "B", priority: 9),
                new FakeItem("a_lo", section: "A", priority: 1),
                new FakeItem("b_lo", section: "B", priority: 2),
                new FakeItem("a_hi", section: "A", priority: 8)
            };

            var snap = SnapshotFlattener.Flatten(Inputs(
                items,
                new NaturalSectionComparer(),                              // sections A then B
                new CompositeComparer<FakeItem>(new[] { PriorityKey() }, SortDirection.Ascending),
                new FilterPipeline<FakeItem>()));

            // Sections: A (a_lo, a_hi) then B (b_lo, b_hi). Item sort never crosses the boundary.
            CollectionAssert.AreEqual(new[] { "a_lo", "a_hi", "b_lo", "b_hi" }, ItemIdsInOrder(snap));

            Assert.AreEqual("A", snap.Rows[0].SectionKey);
            Assert.AreEqual(RowKind.SectionHeader, snap.Rows[0].Kind);
        }

        [Test]
        public void SectionComparerIsIndependentOfItemDirection()
        {
            var items = new List<FakeItem>
            {
                new FakeItem("a1", section: "A", priority: 1),
                new FakeItem("a2", section: "A", priority: 2),
                new FakeItem("b1", section: "B", priority: 1)
            };

            // Sections ordered B before A explicitly; items DESCENDING within each section.
            var snap = SnapshotFlattener.Flatten(Inputs(
                items,
                new ExplicitSectionOrderComparer(new object[] { "B", "A" }),
                new CompositeComparer<FakeItem>(new[] { PriorityKey() }, SortDirection.Descending),
                new FilterPipeline<FakeItem>()));

            // B first (b1), then A descending (a2, a1) - section axis independent of item axis.
            CollectionAssert.AreEqual(new[] { "b1", "a2", "a1" }, ItemIdsInOrder(snap));
        }

        [Test]
        public void FilterAppliesInsideEachSection()
        {
            var items = new List<FakeItem>
            {
                new FakeItem("a_keep", section: "A", priority: 5),
                new FakeItem("a_drop", section: "A", priority: 0),
                new FakeItem("b_keep", section: "B", priority: 5)
            };
            var filter = new FilterPipeline<FakeItem>();
            filter.Add(x => x.Priority > 0);

            var snap = SnapshotFlattener.Flatten(Inputs(
                items,
                new NaturalSectionComparer(),
                new CompositeComparer<FakeItem>(new[] { PriorityKey() }, SortDirection.Ascending),
                filter));

            CollectionAssert.AreEqual(new[] { "a_keep", "b_keep" }, ItemIdsInOrder(snap));
            Assert.AreEqual(2, snap.VisibleItemCount);
        }
    }
}
