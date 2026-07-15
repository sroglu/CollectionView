using System.Collections.Generic;

namespace PFound.CollectionView.Sorting
{
    /// <summary>
    /// Registry of the sort modes available for a collection. Also builds the
    /// <see cref="CompositeComparer{T}"/> for a (mode, direction) pair. Pure POCO.
    /// </summary>
    public sealed class SortRegistry<T>
    {
        readonly Dictionary<string, SortMode<T>> _modes = new Dictionary<string, SortMode<T>>();
        readonly List<SortMode<T>> _order = new List<SortMode<T>>();

        public IReadOnlyList<SortMode<T>> Modes => _order;

        public bool HasMode(string id) => _modes.ContainsKey(id);

        public void Register(SortMode<T> mode)
        {
            if (!_modes.ContainsKey(mode.Id))
            {
                _order.Add(mode);
            }
            _modes[mode.Id] = mode;
        }

        public SortMode<T> Get(string id) => _modes[id];

        /// <summary>Builds a composite comparer for the given mode id and direction.</summary>
        public CompositeComparer<T> ComparerFor(string modeId, SortDirection direction)
            => new CompositeComparer<T>(_modes[modeId].Keys, direction);
    }

    /// <summary>
    /// Caches the ascending-sorted view of a stable source under a given sort mode, so repeated
    /// snapshot builds under the same mode do not re-sort. A direction flip reuses the cache by
    /// reversing (no re-sort). Any source mutation calls <see cref="Invalidate"/>.
    /// </summary>
    public sealed class SortCache<T>
    {
        readonly List<T> _ascending = new List<T>();
        readonly List<T> _descending = new List<T>();
        string _cachedModeId;
        bool _hasAscending;
        bool _hasDescending;

        /// <summary>Drops all cached orderings. Call whenever the underlying item set changes.</summary>
        public void Invalidate()
        {
            _hasAscending = false;
            _hasDescending = false;
            _cachedModeId = string.Empty;
        }

        /// <summary>
        /// Returns the source sorted by (mode, direction). Ascending is computed with the
        /// comparer and cached; descending is produced by reversing the ascending cache.
        /// </summary>
        public IReadOnlyList<T> GetSorted(IReadOnlyList<T> source, string modeId, CompositeComparer<T> ascendingComparer, SortDirection direction)
        {
            bool sameMode = _hasAscending && string.Equals(_cachedModeId, modeId);
            if (!sameMode)
            {
                _ascending.Clear();
                _ascending.AddRange(source);
                _ascending.Sort(ascendingComparer);
                _cachedModeId = modeId;
                _hasAscending = true;
                _hasDescending = false;
            }

            if (direction == SortDirection.Ascending)
            {
                return _ascending;
            }

            if (!_hasDescending)
            {
                _descending.Clear();
                for (int i = _ascending.Count - 1; i >= 0; i--)
                {
                    _descending.Add(_ascending[i]);
                }
                _hasDescending = true;
            }
            return _descending;
        }
    }
}
