using System;

namespace PFound.CollectionView.Items
{
    /// <summary>
    /// Optional role for items that participate in sorting. Exposes one or more
    /// named, comparable sort-key values. A generic sort key can then compare two
    /// items by asking each for the value behind a given key id, without the core
    /// knowing any concrete attribute.
    /// </summary>
    /// <remarks>
    /// Items may also be sorted by bespoke <see cref="Sorting.ISortKey{T}"/> implementations
    /// that read concrete fields directly; implementing <see cref="IOrderable"/> is only
    /// required to reuse the built-in key-name driven comparer.
    /// </remarks>
    public interface IOrderable
    {
        /// <summary>
        /// Returns the comparable value for a named ordering attribute
        /// (e.g. "name", "created", "priority"). Must return a non-null
        /// <see cref="IComparable"/> for every key id the host registers.
        /// </summary>
        IComparable GetOrderValue(string keyId);
    }
}
