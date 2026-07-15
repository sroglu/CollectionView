using System;
using PFound.CollectionView.Items;

namespace PFound.CollectionView.Tests
{
    /// <summary>
    /// Test double implementing every optional role, so a single type exercises identity, ordering,
    /// grouping and state without a scene.
    /// </summary>
    internal sealed class FakeItem : ICollectionItem, IOrderable, IGroupable, ICellState
    {
        public FakeItem(string id, string section = "s", int priority = 0, string name = "", int intra = 0, object state = null)
        {
            Id = id;
            Section = section;
            Priority = priority;
            Name = name;
            Intra = intra;
            State = state;
        }

        public string Id { get; }
        public string Section { get; }
        public int Priority { get; }
        public string Name { get; }
        public int Intra { get; }
        public object State { get; }

        public object IdentityKey => Id;
        public object SectionKey => Section;
        public int IntraSectionOrder => Intra;
        public object StateToken => State;

        public IComparable GetOrderValue(string keyId)
        {
            switch (keyId)
            {
                case "priority": return Priority;
                case "name": return Name;
                case "intra": return Intra;
                default: return Id;
            }
        }

        public override string ToString() => Id;
    }
}
