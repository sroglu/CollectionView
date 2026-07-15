using System;
using System.Collections.Generic;
using PFound.CollectionView.Config;
using PFound.CollectionView.Filtering;
using PFound.CollectionView.Items;
using PFound.CollectionView.Snapshot;
using PFound.CollectionView.Sorting;

namespace PFound.CollectionView.Model
{
    /// <summary>
    /// The single canonical model. Flat and grouped are the SAME implementation configured differently:
    /// grouping off -> one implicit section, headers suppressed. Owns items, sort selection, filter
    /// pipeline, and (grouped) sections + expansion. Produces an immutable <see cref="CollectionSnapshot"/>
    /// (row space) plus a filter-aware item-space API (<see cref="Count"/>/<see cref="ItemAt"/>/
    /// <see cref="IndexOf"/>). Pure POCO - no scene, no scroller.
    /// </summary>
    public sealed class CollectionModel<T> : IGroupedCollectionModel<T>, ISnapshotSource, IColumnSink, IExpansionToggle where T : ICollectionItem
    {
        static readonly object DefaultSectionKey = new object();

        readonly List<T> _source = new List<T>();
        readonly SortCache<T> _sortCache = new SortCache<T>();

        readonly List<T> _visibleItems = new List<T>();
        readonly Dictionary<object, int> _visibleIndex = new Dictionary<object, int>();

        readonly FlattenSettings _settings;

        string _activeModeId = string.Empty;
        bool _hasSort;
        SortDirection _direction = SortDirection.Ascending;

        bool _snapshotDirty = true;
        CollectionSnapshot _snapshot = CollectionSnapshot.Empty;

        Func<T, object> _sectionKeySelector;
        Func<object, object> _sectionHeaderProvider;
        ISectionComparer _sectionComparer = new NaturalSectionComparer();

        public CollectionModel() : this(new FlattenSettings())
        {
        }

        public CollectionModel(FlattenSettings settings)
        {
            _settings = settings;
            _sectionKeySelector = _ => DefaultSectionKey;
            _sectionHeaderProvider = _ => null;
            Filter = new FilterPipeline<T>();
            Filter.Changed += OnFilterChanged;
        }

        public event Action Changed;

        /// <summary>The composable filter pipeline (Sec 4). Mutating it invalidates the visible space.</summary>
        public FilterPipeline<T> Filter { get; }

        /// <summary>Registerable multi-key sort modes (Sec 5).</summary>
        public SortRegistry<T> SortRegistry { get; } = new SortRegistry<T>();

        /// <summary>Per-section collapse/expand state (grouped mode).</summary>
        public ExpansionState Expansion { get; } = new ExpansionState();

        public bool Grouping
        {
            get => _settings.Grouping;
            set { if (_settings.Grouping != value) { _settings.Grouping = value; MarkStructureDirty(); } }
        }

        // ---- source ----

        public void SetItems(IEnumerable<T> items)
        {
            _source.Clear();
            _source.AddRange(items);
            _sortCache.Invalidate();
            MarkStructureDirty();
        }

        // ---- sort selection ----

        public void SetSortMode(string modeId, SortDirection direction)
        {
            _activeModeId = modeId;
            _hasSort = SortRegistry.HasMode(modeId);
            _direction = direction;
            MarkStructureDirty();
        }

        public void SetDirection(SortDirection direction)
        {
            _direction = direction;
            MarkStructureDirty();
        }

        public void ClearSort()
        {
            _hasSort = false;
            MarkStructureDirty();
        }

        // ---- grouping configuration ----

        public void SetSectionKeySelector(Func<T, object> selector)
        {
            _sectionKeySelector = selector;
            MarkStructureDirty();
        }

        public void SetSectionHeaderProvider(Func<object, object> provider)
        {
            _sectionHeaderProvider = provider;
            MarkStructureDirty();
        }

        public void SetSectionComparer(ISectionComparer comparer)
        {
            _sectionComparer = comparer;
            MarkStructureDirty();
        }

        public void SetColumns(int columns)
        {
            _settings.Columns = columns < 1 ? 1 : columns;
            MarkStructureDirty();
        }

        /// <summary>Flips a section's collapsed state and rebuilds (Sec 11c).</summary>
        public void ToggleSection(object sectionKey)
        {
            Expansion.Toggle(sectionKey);
            MarkStructureDirty();
        }

        // ---- item-space API (Sec 2, filter-aware) ----

        public int Count
        {
            get { EnsureBuilt(); return _visibleItems.Count; }
        }

        public T ItemAt(int index)
        {
            EnsureBuilt();
            return _visibleItems[index];
        }

        public int IndexOf(object identityKey)
        {
            EnsureBuilt();
            return _visibleIndex.TryGetValue(identityKey, out int idx) ? idx : -1;
        }

        // ---- snapshot (row space) ----

        public CollectionSnapshot BuildSnapshot()
        {
            EnsureBuilt();
            return _snapshot;
        }

        void EnsureBuilt()
        {
            if (!_snapshotDirty)
            {
                return;
            }

            IReadOnlyList<T> ordered;
            if (_hasSort)
            {
                var ascending = SortRegistry.ComparerFor(_activeModeId, SortDirection.Ascending);
                ordered = _sortCache.GetSorted(_source, _activeModeId, ascending, _direction);
            }
            else
            {
                ordered = _source;
            }

            var input = new FlattenInputs<T>
            {
                Items = ordered,
                Settings = _settings,
                Expansion = Expansion,
                Filter = Filter,
                Sorted = false, // items already globally ordered; partition preserves order (Sec 11e)
                SectionKeySelector = _sectionKeySelector,
                SectionHeaderProvider = _sectionHeaderProvider,
                SectionComparer = _sectionComparer
            };

            _snapshot = SnapshotFlattener.Flatten(input);
            RebuildVisibleIndex();
            _snapshotDirty = false;
        }

        void RebuildVisibleIndex()
        {
            _visibleItems.Clear();
            _visibleIndex.Clear();
            var rows = _snapshot.Rows;
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].Kind != RowKind.ItemRow)
                {
                    continue;
                }
                var items = rows[i].Items;
                for (int j = 0; j < items.Count; j++)
                {
                    var item = (T)items[j];
                    _visibleIndex[item.IdentityKey] = _visibleItems.Count;
                    _visibleItems.Add(item);
                }
            }
        }

        void OnFilterChanged()
        {
            // Filter change keeps the sort cache valid (source unchanged) - only re-filter + re-flatten.
            MarkStructureDirty();
        }

        void MarkStructureDirty()
        {
            _snapshotDirty = true;
            Changed?.Invoke();
        }
    }
}
