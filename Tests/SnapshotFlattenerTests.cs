using System.Collections.Generic;
using NUnit.Framework;
using PFound.CollectionView.Config;
using PFound.CollectionView.Filtering;
using PFound.CollectionView.Items;
using PFound.CollectionView.Model;
using PFound.CollectionView.Snapshot;
using PFound.CollectionView.Sorting;

namespace PFound.CollectionView.Tests
{
    public sealed class SnapshotFlattenerTests
    {
        static FlattenInputs<FakeItem> BaseInputs(IReadOnlyList<FakeItem> items, FlattenSettings settings)
        {
            return new FlattenInputs<FakeItem>
            {
                Items = items,
                Settings = settings,
                Expansion = new ExpansionState(),
                Filter = new FilterPipeline<FakeItem>(),
                Sorted = false,
                SectionKeySelector = x => x.Section,
                SectionHeaderProvider = _ => null,
                SectionComparer = new NaturalSectionComparer()
            };
        }

        static int CountKind(CollectionSnapshot snap, RowKind kind)
        {
            int n = 0;
            for (int i = 0; i < snap.Rows.Count; i++)
            {
                if (snap.Rows[i].Kind == kind) n++;
            }
            return n;
        }

        [Test]
        public void Flat_ChunksIntoRowsByColumns()
        {
            var items = new List<FakeItem>
            {
                new FakeItem("a"), new FakeItem("b"), new FakeItem("c"),
                new FakeItem("d"), new FakeItem("e")
            };
            var snap = SnapshotFlattener.Flatten(BaseInputs(items, new FlattenSettings { Grouping = false, Columns = 2 }));

            Assert.AreEqual(3, snap.Rows.Count, "5 items / 2 columns -> 3 rows");
            Assert.AreEqual(5, snap.VisibleItemCount);
            Assert.AreEqual(2, snap.Rows[0].Items.Count);
            Assert.AreEqual(1, snap.Rows[2].Items.Count, "last row holds the remainder");
            Assert.AreEqual(0, CountKind(snap, RowKind.SectionHeader), "flat mode emits no headers");
        }

        [Test]
        public void Flat_IndexOf_PointsToHostingRow()
        {
            var items = new List<FakeItem> { new FakeItem("a"), new FakeItem("b"), new FakeItem("c") };
            var snap = SnapshotFlattener.Flatten(BaseInputs(items, new FlattenSettings { Grouping = false, Columns = 2 }));
            Assert.AreEqual(0, snap.IndexOf["a"]);
            Assert.AreEqual(0, snap.IndexOf["b"]);
            Assert.AreEqual(1, snap.IndexOf["c"], "third item is on the second row");
        }

        [Test]
        public void Flat_Empty_ProducesEmptySnapshot()
        {
            var snap = SnapshotFlattener.Flatten(BaseInputs(new List<FakeItem>(), new FlattenSettings { Grouping = false, Columns = 3 }));
            Assert.AreEqual(0, snap.Rows.Count);
            Assert.IsTrue(snap.IsEmpty);
        }

        [Test]
        public void Grouped_EmitsHeaderThenItemRowsPerSection()
        {
            var items = new List<FakeItem>
            {
                new FakeItem("a1", section: "A"), new FakeItem("a2", section: "A"),
                new FakeItem("b1", section: "B")
            };
            var snap = SnapshotFlattener.Flatten(BaseInputs(items, new FlattenSettings { Grouping = true, Columns = 1 }));

            // A header, a1 row, a2 row, B header, b1 row
            Assert.AreEqual(RowKind.SectionHeader, snap.Rows[0].Kind);
            Assert.AreEqual("A", snap.Rows[0].SectionKey);
            Assert.AreEqual(RowKind.ItemRow, snap.Rows[1].Kind);
            Assert.AreEqual(RowKind.ItemRow, snap.Rows[2].Kind);
            Assert.AreEqual(RowKind.SectionHeader, snap.Rows[3].Kind);
            Assert.AreEqual("B", snap.Rows[3].SectionKey);
            Assert.AreEqual(3, snap.VisibleItemCount);
        }

        [Test]
        public void Grouped_Collapsed_KeepsHeaderDropsMembers()
        {
            var items = new List<FakeItem>
            {
                new FakeItem("a1", section: "A"), new FakeItem("a2", section: "A"),
                new FakeItem("b1", section: "B")
            };
            var input = BaseInputs(items, new FlattenSettings { Grouping = true, Columns = 1 });
            var expansion = new ExpansionState();
            expansion.SetCollapsed("A", true);
            input.Expansion = expansion;

            var snap = SnapshotFlattener.Flatten(input);

            Assert.AreEqual(2, CountKind(snap, RowKind.SectionHeader), "both headers remain");
            Assert.AreEqual(1, snap.VisibleItemCount, "collapsed A's members drop out of the count");
            var header = (SectionHeaderData)snap.Rows[0].HeaderData;
            Assert.IsTrue(header.IsCollapsed);
            Assert.AreEqual(2, header.VisibleCount, "rollup still reports A's post-filter count");
        }

        [Test]
        public void HeaderRollup_ReportsPostFilterVisibleCount()
        {
            var items = new List<FakeItem>
            {
                new FakeItem("a1", section: "A", priority: 1),
                new FakeItem("a2", section: "A", priority: 0)
            };
            var input = BaseInputs(items, new FlattenSettings { Grouping = true, Columns = 1 });
            input.Filter.Add(x => x.Priority > 0); // removes a2

            var snap = SnapshotFlattener.Flatten(input);
            var header = (SectionHeaderData)snap.Rows[0].HeaderData;
            Assert.AreEqual(1, header.VisibleCount);
            Assert.AreEqual(2, header.TotalCount);
        }

        [Test]
        public void EmptyPolicy_Hide_OmitsFilteredEmptySection()
        {
            var items = new List<FakeItem> { new FakeItem("a1", section: "A", priority: 0) };
            var input = BaseInputs(items, new FlattenSettings { Grouping = true, Columns = 1, EmptyPolicy = EmptySectionPolicy.Hide });
            input.Filter.Add(x => x.Priority > 0); // A filters to empty

            var snap = SnapshotFlattener.Flatten(input);
            Assert.AreEqual(0, snap.Rows.Count, "Hide omits header + placeholder");
            Assert.IsTrue(snap.IsEmpty);
        }

        [Test]
        public void EmptyPolicy_ShowIfFilteredEmpty_ShowsFilteredButHidesStructural()
        {
            // A has an item that filters away (filtered-empty); Z is declared but has no items (structural).
            var items = new List<FakeItem> { new FakeItem("a1", section: "A", priority: 0) };
            var input = BaseInputs(items, new FlattenSettings
            {
                Grouping = true,
                Columns = 1,
                EmptyPolicy = EmptySectionPolicy.ShowIfFilteredEmpty
            });
            input.DeclaredSectionKeys = new object[] { "A", "Z" };
            input.SectionComparer = new ExplicitSectionOrderComparer(new object[] { "A", "Z" });
            input.Filter.Add(x => x.Priority > 0);

            var snap = SnapshotFlattener.Flatten(input);

            Assert.AreEqual(1, CountKind(snap, RowKind.SectionHeader), "only the filtered-empty A shows");
            Assert.AreEqual("A", snap.Rows[0].SectionKey);
            Assert.AreEqual(1, CountKind(snap, RowKind.SectionEmpty));
        }

        [Test]
        public void EmptyPolicy_ShowPlaceholder_ShowsEveryDeclaredSection()
        {
            var items = new List<FakeItem> { new FakeItem("a1", section: "A") };
            var input = BaseInputs(items, new FlattenSettings
            {
                Grouping = true,
                Columns = 1,
                EmptyPolicy = EmptySectionPolicy.ShowPlaceholder
            });
            input.DeclaredSectionKeys = new object[] { "A", "Z" };
            input.SectionComparer = new ExplicitSectionOrderComparer(new object[] { "A", "Z" });

            var snap = SnapshotFlattener.Flatten(input);

            Assert.AreEqual(2, CountKind(snap, RowKind.SectionHeader), "structurally-empty Z still shows its header");
            Assert.AreEqual(1, CountKind(snap, RowKind.SectionEmpty), "Z emits an inline empty placeholder");
        }
    }
}
