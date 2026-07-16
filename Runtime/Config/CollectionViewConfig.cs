using System;
using System.Collections.Generic;
using PFound.CollectionView.Snapshot;
using PFound.CollectionView.Sorting;
using UnityEngine;

namespace PFound.CollectionView.Config
{
    /// <summary>
    /// Per-screen description of a collection view: templates, grouping, sort modes, filters, chrome,
    /// selection, and layout - data + <c>[SerializeReference]</c> strategy slots, no imperative setters.
    /// A new screen is a new config asset, not an edit to the shared view. Assets are CDN-shippable on the
    /// host side; the core module stays agnostic. Generic <see cref="ISortKey{T}"/> / <see cref="Predicate{T}"/>
    /// stay code-registered - this asset carries the data-authored (<see cref="ICollectionItem"/>-typed) forms.
    /// </summary>
    [CreateAssetMenu(menuName = "PFound/CollectionView/Collection View Config", fileName = "CollectionViewConfig")]
    public sealed class CollectionViewConfig : ScriptableObject
    {
        // ---- Templates (feature A: heterogeneous by key) ----
        [SerializeField] TemplatePrefabEntry[] _rowTemplates = Array.Empty<TemplatePrefabEntry>();
        [SerializeField] TemplatePrefabEntry[] _itemCellTemplates = Array.Empty<TemplatePrefabEntry>();

        // ---- Layout ----
        [SerializeField] float _minItemWidth = 200f;
        [SerializeField] float _spacing = 8f;
        [SerializeField] float _designItemWidth = 200f;
        [SerializeField] int _bufferRows = 2;
        [SerializeField] bool _useFixedColumns;
        [SerializeField] int _fixedColumns = 1;
        [SerializeField] int _minColumns = 1;   // responsive floor: never fewer than N items per row

        // ---- Grouping ----
        [SerializeField] bool _grouping;
        [SerializeField] DefaultExpansion _defaultExpansion = DefaultExpansion.AllExpanded;
        [SerializeField] EmptySectionPolicy _emptySectionPolicy = EmptySectionPolicy.Hide;
        [SerializeField] string _headerTemplateKey = CollectionTemplateKeys.Header;
        [SerializeField] string _emptyTemplateKey = CollectionTemplateKeys.Empty;

        // ---- Sorting (data-authored; generic keys are code-registered) ----
        [SerializeField] SortModeConfig[] _sortModes = Array.Empty<SortModeConfig>();
        [SerializeReference] List<SortKeyStrategy> _sortKeys = new List<SortKeyStrategy>();
        [SerializeField] string _defaultSortModeId = string.Empty;
        [SerializeField] SortDirection _defaultDirection = SortDirection.Ascending;

        // ---- Filtering (data-authored) ----
        [SerializeReference] List<ItemFilterStrategy> _filters = new List<ItemFilterStrategy>();

        // ---- Async content loader (optional, bool-gated) ----
        [SerializeField] bool _useContentLoader;
        [SerializeReference] CellContentLoaderStrategy _contentLoader;

        // ---- Chrome flags ----
        [SerializeField] bool _showSortDropdown;
        [SerializeField] bool _showDirectionToggle;
        [SerializeField] bool _showEmptyState = true;
        [SerializeField] bool _showScrollProgress;
        [SerializeField] bool _useEmptySlotPadding;
        [SerializeField] int _emptySlotCapacity;

        // ---- Selection ----
        [SerializeField] SelectionMode _selectionMode = SelectionMode.None;

        // ---- Accessors ----
        public float MinItemWidth => _minItemWidth;
        public float Spacing => _spacing;
        public float DesignItemWidth => _designItemWidth;
        public int BufferRows => _bufferRows;
        public bool UseFixedColumns => _useFixedColumns;
        public int FixedColumns => _fixedColumns;
        public int MinColumns => _minColumns;

        public bool Grouping => _grouping;
        public DefaultExpansion DefaultExpansion => _defaultExpansion;
        public EmptySectionPolicy EmptySectionPolicy => _emptySectionPolicy;
        public string HeaderTemplateKey => _headerTemplateKey;
        public string EmptyTemplateKey => _emptyTemplateKey;

        public IReadOnlyList<SortModeConfig> SortModes => _sortModes;
        public IReadOnlyList<SortKeyStrategy> SortKeys => _sortKeys;
        public string DefaultSortModeId => _defaultSortModeId;
        public SortDirection DefaultDirection => _defaultDirection;

        public IReadOnlyList<ItemFilterStrategy> Filters => _filters;

        public bool UseContentLoader => _useContentLoader;

        public bool ShowSortDropdown => _showSortDropdown;
        public bool ShowDirectionToggle => _showDirectionToggle;
        public bool ShowEmptyState => _showEmptyState;
        public bool ShowScrollProgress => _showScrollProgress;
        public bool UseEmptySlotPadding => _useEmptySlotPadding;
        public int EmptySlotCapacity => _emptySlotCapacity;

        public SelectionMode SelectionMode => _selectionMode;

        /// <summary>Resolves the content loader strategy. Only valid when <see cref="UseContentLoader"/> is true.</summary>
        public CellContentLoaderStrategy ContentLoader
        {
            get
            {
                Debug.Assert(_useContentLoader, "ContentLoader requested while _useContentLoader is false.", this);
                return _contentLoader;
            }
        }

        public bool TryGetRowTemplate(string key, out GameObject prefab) => TryGet(_rowTemplates, key, out prefab);

        public bool TryGetItemCellTemplate(string key, out GameObject prefab) => TryGet(_itemCellTemplates, key, out prefab);

        static bool TryGet(TemplatePrefabEntry[] table, string key, out GameObject prefab)
        {
            for (int i = 0; i < table.Length; i++)
            {
                if (string.Equals(table[i].TemplateKey, key))
                {
                    prefab = table[i].Prefab;
                    return true;
                }
            }
            prefab = null;
            return false;
        }

        void OnValidate()
        {
            if (_useFixedColumns)
            {
                Debug.Assert(_fixedColumns >= 1, $"{name}: _fixedColumns must be >= 1 when _useFixedColumns is on.", this);
            }
            if (_useEmptySlotPadding)
            {
                Debug.Assert(_emptySlotCapacity > 0, $"{name}: _emptySlotCapacity must be > 0 when _useEmptySlotPadding is on.", this);
            }
            if (_useContentLoader)
            {
                Debug.Assert(_contentLoader != null, $"{name}: _contentLoader must be assigned when _useContentLoader is on.", this);
            }
        }

        [Serializable]
        public struct TemplatePrefabEntry
        {
            public string TemplateKey;
            public GameObject Prefab;
        }

        [Serializable]
        public struct SortModeConfig
        {
            public string Id;
            public string LabelKey;
            public string[] KeyIds;
            public SortDirection DefaultDirection;
        }
    }
}
