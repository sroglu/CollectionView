using System;
using System.Collections.Generic;
using PFound.CollectionView.Items;

namespace PFound.CollectionView.Sorting
{
    /// <summary>
    /// Convenience <see cref="ISortKey{T}"/> built from a projection to a comparable value.
    /// The value is extracted once per comparison; the natural <see cref="IComparable"/> order applies.
    /// </summary>
    public sealed class DelegateSortKey<T> : ISortKey<T>
    {
        readonly Func<T, IComparable> _project;

        public DelegateSortKey(string id, Func<T, IComparable> project)
        {
            Id = id;
            _project = project;
        }

        public string Id { get; }

        public int Compare(T a, T b)
        {
            var va = _project(a);
            var vb = _project(b);
            return Comparer<IComparable>.Default.Compare(va, vb);
        }
    }

    /// <summary>
    /// <see cref="ISortKey{T}"/> that reads a named comparable value off items implementing
    /// <see cref="IOrderable"/>. Lets the host register keys purely by attribute name.
    /// </summary>
    public sealed class OrderableSortKey<T> : ISortKey<T> where T : IOrderable
    {
        public OrderableSortKey(string keyId)
        {
            Id = keyId;
        }

        public string Id { get; }

        public int Compare(T a, T b)
        {
            var va = a.GetOrderValue(Id);
            var vb = b.GetOrderValue(Id);
            return Comparer<IComparable>.Default.Compare(va, vb);
        }
    }

    /// <summary>
    /// Built-in intra-section order key over <see cref="IGroupable.IntraSectionOrder"/>.
    /// Registerable so a section can be ordered by its authored member order.
    /// </summary>
    public sealed class IntraSectionOrderKey<T> : ISortKey<T> where T : IGroupable
    {
        public const string KeyId = "intra_section_order";

        public string Id => KeyId;

        public int Compare(T a, T b) => a.IntraSectionOrder.CompareTo(b.IntraSectionOrder);
    }
}
