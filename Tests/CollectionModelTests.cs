using System.Collections.Generic;
using NUnit.Framework;
using PFound.CollectionView.Model;
using PFound.CollectionView.Sorting;

namespace PFound.CollectionView.Tests
{
    public sealed class CollectionModelTests
    {
        static CollectionModel<FakeItem> BuildModel()
        {
            var model = new CollectionModel<FakeItem>();
            model.SortRegistry.Register(new SortMode<FakeItem>("byPriority",
                new[] { new DelegateSortKey<FakeItem>("priority", x => x.Priority) }));
            model.SetItems(new List<FakeItem>
            {
                new FakeItem("c", priority: 3),
                new FakeItem("a", priority: 1),
                new FakeItem("b", priority: 2)
            });
            model.SetSortMode("byPriority", SortDirection.Ascending);
            return model;
        }

        [Test]
        public void ItemSpace_IsSortedAndIndexable()
        {
            var model = BuildModel();
            Assert.AreEqual(3, model.Count);
            Assert.AreEqual("a", model.ItemAt(0).Id);
            Assert.AreEqual("b", model.ItemAt(1).Id);
            Assert.AreEqual("c", model.ItemAt(2).Id);
            Assert.AreEqual(1, model.IndexOf("b"));
        }

        [Test]
        public void DirectionFlip_ReversesOrder()
        {
            var model = BuildModel();
            model.SetDirection(SortDirection.Descending);
            Assert.AreEqual("c", model.ItemAt(0).Id);
            Assert.AreEqual("a", model.ItemAt(2).Id);
        }

        [Test]
        public void Filter_IsAppliedToItemSpace_AndRaisesChange()
        {
            var model = BuildModel();
            int changes = 0;
            model.Changed += () => changes++;

            model.Filter.Add(x => x.Priority >= 2);

            Assert.GreaterOrEqual(changes, 1, "filter mutation raises Changed");
            Assert.AreEqual(2, model.Count);
            Assert.AreEqual(-1, model.IndexOf("a"), "filtered-out item is not in the visible space");
            Assert.AreEqual(0, model.IndexOf("b"));
        }

        [Test]
        public void Snapshot_RowCountMatchesColumns()
        {
            var model = BuildModel();
            model.SetColumns(2);
            var snap = model.BuildSnapshot();
            Assert.AreEqual(2, snap.Rows.Count, "3 items / 2 columns -> 2 rows");
            Assert.AreEqual(3, snap.VisibleItemCount);
        }
    }
}
