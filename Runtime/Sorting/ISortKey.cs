namespace PFound.CollectionView.Sorting
{
    /// <summary>
    /// A named comparer over a single ordering attribute. Registerable by the caller;
    /// the core ships no fixed key set. Compose several into a <see cref="SortMode{T}"/>
    /// to get primary -> tie-break ordering.
    /// </summary>
    public interface ISortKey<in T>
    {
        /// <summary>Stable id used to reference this key from a sort mode / config.</summary>
        string Id { get; }

        /// <summary>
        /// Ascending comparison of two items on this key alone.
        /// Negative -> a before b, zero -> tie (defer to the next key), positive -> a after b.
        /// </summary>
        int Compare(T a, T b);
    }
}
