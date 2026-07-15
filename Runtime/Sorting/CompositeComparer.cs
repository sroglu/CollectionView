using System.Collections.Generic;

namespace PFound.CollectionView.Sorting
{
    /// <summary>
    /// Evaluates an ordered list of <see cref="ISortKey{T}"/> in sequence, returning the first
    /// non-zero comparison (primary -> tie-breakers). Direction is orthogonal: it negates the
    /// whole composite result, which is equivalent to reversing the sorted order.
    /// </summary>
    public sealed class CompositeComparer<T> : IComparer<T>
    {
        readonly IReadOnlyList<ISortKey<T>> _keys;
        readonly int _sign;

        public CompositeComparer(IReadOnlyList<ISortKey<T>> keys, SortDirection direction)
        {
            _keys = keys;
            _sign = direction == SortDirection.Descending ? -1 : 1;
        }

        public SortDirection Direction => _sign < 0 ? SortDirection.Descending : SortDirection.Ascending;

        public int Compare(T a, T b)
        {
            for (int i = 0; i < _keys.Count; i++)
            {
                int c = _keys[i].Compare(a, b);
                if (c != 0)
                {
                    return _sign * c;
                }
            }
            return 0;
        }
    }
}
