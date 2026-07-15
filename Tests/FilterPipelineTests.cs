using NUnit.Framework;
using PFound.CollectionView.Filtering;

namespace PFound.CollectionView.Tests
{
    public sealed class FilterPipelineTests
    {
        [Test]
        public void EmptyPipeline_MatchesEverything()
        {
            var pipe = new FilterPipeline<FakeItem>();
            Assert.IsTrue(pipe.Matches(new FakeItem("a")));
        }

        [Test]
        public void AndPredicates_RequireAllToPass()
        {
            var pipe = new FilterPipeline<FakeItem>();
            pipe.Add(x => x.Priority > 0);
            pipe.Add(x => x.Name == "keep");

            Assert.IsTrue(pipe.Matches(new FakeItem("a", priority: 5, name: "keep")));
            Assert.IsFalse(pipe.Matches(new FakeItem("b", priority: 5, name: "drop")), "second AND predicate fails");
            Assert.IsFalse(pipe.Matches(new FakeItem("c", priority: 0, name: "keep")), "first AND predicate fails");
        }

        [Test]
        public void OrGroup_PassesWhenAnyMemberPasses()
        {
            var pipe = new FilterPipeline<FakeItem>();
            var group = pipe.AddOrGroup();
            group.Add(x => x.Section == "red");
            group.Add(x => x.Section == "blue");

            Assert.IsTrue(pipe.Matches(new FakeItem("a", section: "red")));
            Assert.IsTrue(pipe.Matches(new FakeItem("b", section: "blue")));
            Assert.IsFalse(pipe.Matches(new FakeItem("c", section: "green")), "in no OR-group branch");
        }

        [Test]
        public void AndCombinedWithOrGroup_BothConstraintsApply()
        {
            var pipe = new FilterPipeline<FakeItem>();
            pipe.Add(x => x.Priority > 0);
            var group = pipe.AddOrGroup();
            group.Add(x => x.Section == "red");
            group.Add(x => x.Section == "blue");

            Assert.IsTrue(pipe.Matches(new FakeItem("a", section: "red", priority: 1)));
            Assert.IsFalse(pipe.Matches(new FakeItem("b", section: "red", priority: 0)), "AND fails");
            Assert.IsFalse(pipe.Matches(new FakeItem("c", section: "green", priority: 1)), "OR-group fails");
        }

        [Test]
        public void ExclusionByIdentity_HidesExcludedIds()
        {
            var exclusion = new IdentityExclusionFilter<FakeItem>();
            var pipe = new FilterPipeline<FakeItem>();
            pipe.Add(exclusion.AsPredicate());

            var kept = new FakeItem("keep");
            var hidden = new FakeItem("hide");
            exclusion.Exclude("hide");

            Assert.IsTrue(pipe.Matches(kept));
            Assert.IsFalse(pipe.Matches(hidden));

            exclusion.Include("hide");
            Assert.IsTrue(pipe.Matches(hidden), "re-including restores visibility");
        }

        [Test]
        public void ChangedEvent_FiresOnMutation()
        {
            var pipe = new FilterPipeline<FakeItem>();
            int count = 0;
            pipe.Changed += () => count++;

            var p = pipe.Add(x => true);
            var group = pipe.AddOrGroup();
            group.Add(x => true);
            pipe.Remove(p);
            pipe.Clear();

            Assert.AreEqual(5, count, "add, addOrGroup, group.add, remove, clear each raise once");
        }
    }
}
