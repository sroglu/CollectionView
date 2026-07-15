using System.Collections.Generic;
using NUnit.Framework;
using PFound.CollectionView.Sorting;

namespace PFound.CollectionView.Tests
{
    public sealed class CompositeComparerTests
    {
        static ISortKey<FakeItem> PriorityKey() => new DelegateSortKey<FakeItem>("priority", x => x.Priority);
        static ISortKey<FakeItem> NameKey() => new DelegateSortKey<FakeItem>("name", x => x.Name);

        [Test]
        public void PrimaryKey_OrdersByFirstKey()
        {
            var cmp = new CompositeComparer<FakeItem>(new[] { PriorityKey(), NameKey() }, SortDirection.Ascending);
            var a = new FakeItem("a", priority: 1, name: "z");
            var b = new FakeItem("b", priority: 2, name: "a");
            Assert.Less(cmp.Compare(a, b), 0, "lower priority sorts first regardless of tie-breaker");
        }

        [Test]
        public void TieBreak_UsesSecondKey_WhenPrimaryEqual()
        {
            var cmp = new CompositeComparer<FakeItem>(new[] { PriorityKey(), NameKey() }, SortDirection.Ascending);
            var a = new FakeItem("a", priority: 5, name: "apple");
            var b = new FakeItem("b", priority: 5, name: "banana");
            Assert.Less(cmp.Compare(a, b), 0, "equal priority -> name tie-breaks ascending");
        }

        [Test]
        public void AllKeysEqual_ReturnsZero()
        {
            var cmp = new CompositeComparer<FakeItem>(new[] { PriorityKey(), NameKey() }, SortDirection.Ascending);
            var a = new FakeItem("a", priority: 5, name: "same");
            var b = new FakeItem("b", priority: 5, name: "same");
            Assert.AreEqual(0, cmp.Compare(a, b));
        }

        [Test]
        public void Descending_ReversesWholeComposite_IncludingTieBreak()
        {
            var asc = new CompositeComparer<FakeItem>(new[] { PriorityKey(), NameKey() }, SortDirection.Ascending);
            var desc = new CompositeComparer<FakeItem>(new[] { PriorityKey(), NameKey() }, SortDirection.Descending);
            var a = new FakeItem("a", priority: 5, name: "apple");
            var b = new FakeItem("b", priority: 5, name: "banana");
            Assert.Less(asc.Compare(a, b), 0);
            Assert.Greater(desc.Compare(a, b), 0, "descending negates the tie-broken result too");
        }

        [Test]
        public void SortRegistry_BuildsComparerForModeAndDirection()
        {
            var registry = new SortRegistry<FakeItem>();
            registry.Register(new SortMode<FakeItem>("byPriority", new[] { PriorityKey(), NameKey() }));
            Assert.IsTrue(registry.HasMode("byPriority"));

            var list = new List<FakeItem>
            {
                new FakeItem("a", priority: 3, name: "x"),
                new FakeItem("b", priority: 1, name: "y"),
                new FakeItem("c", priority: 2, name: "z")
            };
            list.Sort(registry.ComparerFor("byPriority", SortDirection.Ascending));
            Assert.AreEqual("b", list[0].Id);
            Assert.AreEqual("c", list[1].Id);
            Assert.AreEqual("a", list[2].Id);
        }
    }
}
