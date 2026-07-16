using System;
using System.Collections.Generic;
using EnhancedUI.EnhancedScroller;
using PFound.CollectionView.Async;
using PFound.CollectionView.Cells;
using PFound.CollectionView.Config;
using PFound.CollectionView.Items;
using PFound.CollectionView.Model;
using PFound.CollectionView.Snapshot;
using UnityEngine;

namespace PFound.CollectionView.View
{
    /// <summary>
    /// The primary view: a thin MonoBehaviour shell implementing <see cref="IEnhancedScrollerDelegate"/>.
    /// It consumes an immutable <see cref="CollectionSnapshot"/> (no index math here), drives the scroller,
    /// pools cells heterogeneously by template key, and preserves/restores scroll on rebuild. All heavy
    /// logic lives in the POCOs it delegates to (model, flattener, layout math, selection).
    /// Named <c>CollectionScrollView</c> to avoid the namespace/type collision the brief calls out.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class CollectionScrollView : MonoBehaviour, IEnhancedScrollerDelegate
    {
        [SerializeField] EnhancedScroller _scroller;
        [SerializeField] CollectionViewConfig _config;
        [SerializeField] RectTransform _viewport;
        [SerializeField] Transform _poolHiddenParent;

        [SerializeField] bool _useEmptyStateView;
        [SerializeField] GameObject _emptyStateView;

        readonly List<RowCellView> _activeRows = new List<RowCellView>();

        ItemCellPool _itemPool;
        SelectionController _selection;
        ISnapshotSource _source;
        IColumnSink _columnSink;
        bool _hasColumnSink;

        Func<object, string> _textResolver;
        Action<ICollectionItem> _onItemSelected;

        IAsyncCellContentLoader _contentLoader;
        bool _hasContentLoader;

        CollectionSnapshot _snapshot = CollectionSnapshot.Empty;
        bool _bound;
        bool _rebuilding;

        // Section collapse/expand should keep the TAPPED header pinned where it is (items fold/unfold
        // below it), so a header toggle anchors the rebuild to that header's on-screen offset. Every other
        // rebuild (data/filter change) keeps the simple normalized-position preserve.
        object _anchorSectionKey;
        float _sectionAnchorDelta;
        bool _hasSectionAnchor;

        /// <summary>Fired when a top/bottom padder shows or hides - drive a scroll-progress indicator/chrome.</summary>
        public event Action<bool, bool> EdgeVisibilityChanged;

        void Awake()
        {
            Debug.Assert(_scroller, $"{name}: _scroller must be wired.", this);
            Debug.Assert(_config, $"{name}: _config must be wired.", this);
            Debug.Assert(_viewport, $"{name}: _viewport must be wired.", this);
            Debug.Assert(_poolHiddenParent, $"{name}: _poolHiddenParent must be wired.", this);
            if (_useEmptyStateView)
            {
                Debug.Assert(_emptyStateView, $"{name}: _emptyStateView required when _useEmptyStateView is on.", this);
            }

            _selection = new SelectionController(_config.SelectionMode);
            _itemPool = new ItemCellPool(ResolveItemCellPrefab, _poolHiddenParent);

            if (_config.UseContentLoader)
            {
                _contentLoader = _config.ContentLoader.Resolve();
                _hasContentLoader = true;
            }

            _scroller.Delegate = this;
            _scroller.cellViewWillRecycle = OnCellWillRecycle;
            _scroller.padderVisibilityChanged = OnPadderVisibilityChanged;
        }

        /// <summary>
        /// Wires the runtime data source and host callbacks, then performs the first build. The
        /// <paramref name="textResolver"/> is the host's localization lookup (the module ships no literal
        /// text); <paramref name="onItemSelected"/> receives taps.
        /// </summary>
        public void Bind(ISnapshotSource source, Func<object, string> textResolver, Action<ICollectionItem> onItemSelected)
        {
            Debug.Assert(source != null, $"{name}: source is required.", this);
            Debug.Assert(textResolver != null, $"{name}: textResolver is required.", this);
            Debug.Assert(onItemSelected != null, $"{name}: onItemSelected is required.", this);

            _source = source;
            _textResolver = textResolver;
            _onItemSelected = onItemSelected;
            _columnSink = source as IColumnSink;
            _hasColumnSink = _columnSink != null;
            _bound = true;

            source.Changed += OnSourceChanged;
            Rebuild();
        }

        void OnDestroy()
        {
            if (_bound)
            {
                _source.Changed -= OnSourceChanged;
            }
        }

        // ---- IEnhancedScrollerDelegate ----

        public int GetNumberOfCells(EnhancedScroller scroller) => _snapshot.Rows.Count;

        public float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
        {
            var row = _snapshot.Rows[dataIndex];
            float fromPrefab = MeasureRowTemplateHeight(row.TemplateKey);
            return fromPrefab > 0f ? fromPrefab : row.Height;
        }

        // Row size is driven by the row's template prefab so heights track the prefab, not a magic
        // number: prefer an explicit LayoutElement.preferredHeight, else the RectTransform height,
        // else fall back to the snapshot row's Height.
        float MeasureRowTemplateHeight(string templateKey)
        {
            if (!_config.TryGetRowTemplate(templateKey, out GameObject prefab)) return 0f;
            var layout = prefab.GetComponent<UnityEngine.UI.LayoutElement>();
            if (layout && layout.preferredHeight > 0f) return layout.preferredHeight;
            var rt = prefab.transform as RectTransform;
            return rt ? rt.rect.height : 0f;
        }

        public EnhancedScrollerCellView GetCellView(EnhancedScroller scroller, int dataIndex, int cellIndex)
        {
            var row = _snapshot.Rows[dataIndex];
            var prefabCell = ResolveRowPrefabCell(row.TemplateKey);
            var cellView = scroller.GetCellView(prefabCell);

            switch (row.Kind)
            {
                case RowKind.ItemRow:
                    var rowCell = (RowCellView)cellView;
                    rowCell.BindRow(row, _itemPool, BuildContext);
                    _activeRows.Add(rowCell);
                    break;
                case RowKind.SectionHeader:
                    var header = (ISectionHeaderCellView)cellView;
                    header.BindHeader((SectionHeaderData)row.HeaderData, _textResolver, OnHeaderToggle);
                    break;
                case RowKind.SectionEmpty:
                    var empty = (ISectionEmptyCellView)cellView;
                    empty.BindEmpty(row.SectionKey, _textResolver);
                    break;
            }

            return cellView;
        }

        // ---- public operations ----

        /// <summary>Rebuilds from a fresh snapshot, preserving scroll position (Sec 3, Sec 11c).</summary>
        public void Rebuild()
        {
            if (!_bound)
            {
                return;
            }

            _rebuilding = true;
            _activeRows.Clear();

            if (_hasColumnSink)
            {
                _columnSink.SetColumns(ComputeColumns());
            }
            _snapshot = _source.BuildSnapshot();

            UpdateEmptyState();
            if (_hasSectionAnchor)
            {
                // A section header was tapped: pin THAT header at the same on-screen offset so it folds/
                // unfolds in place. (Collapsing while scrolled to the very bottom still repositions because
                // the shrunk content can no longer scroll that far - that clamp is inherent, not a bug.)
                _hasSectionAnchor = false;
                _scroller.ReloadData();
                int hIdx = FindHeaderRowIndex(_anchorSectionKey);
                if (hIdx >= 0 && _scroller.ScrollSize > 0f)
                {
                    float pos = _scroller.GetScrollPositionForDataIndex(hIdx, EnhancedScroller.CellViewPositionEnum.Before) + _sectionAnchorDelta;
                    _scroller.SetScrollPositionImmediately(Mathf.Clamp(pos, 0f, _scroller.ScrollSize));
                }
            }
            else
            {
                float positionFactor = _scroller.NormalizedScrollPosition;
                _scroller.ReloadData(positionFactor);
            }
            _rebuilding = false;
        }

        /// <summary>Feature C: re-binds only on-screen rows in place (state/selection change), no reload.</summary>
        public void RefreshVisible()
        {
            for (int i = 0; i < _activeRows.Count; i++)
            {
                var rowCell = _activeRows[i];
                int dataIndex = rowCell.dataIndex;
                if (dataIndex >= 0 && dataIndex < _snapshot.Rows.Count)
                {
                    rowCell.RefreshRow(_snapshot.Rows[dataIndex], BuildContext);
                }
            }
        }

        /// <summary>Scroll-to-item by identity (Sec 11c). O(1) lookup into the materialized snapshot.</summary>
        public void ScrollTo(object identityKey)
        {
            if (_snapshot.IndexOf.TryGetValue(identityKey, out int rowIndex))
            {
                _scroller.JumpToDataIndex(rowIndex);
            }
        }

        public void SetSelected(object identityKey)
        {
            if (_selection.Select(identityKey))
            {
                RefreshVisible();
            }
        }

        public void ClearSelection()
        {
            _selection.Clear();
            RefreshVisible();
        }

        // ---- internals ----

        void OnSourceChanged()
        {
            if (_rebuilding)
            {
                return;
            }
            Rebuild();
        }

        void OnCellWillRecycle(EnhancedScrollerCellView cellView)
        {
            if (cellView is RowCellView rowCell)
            {
                rowCell.RecycleRow();
                _activeRows.Remove(rowCell);
            }
        }

        void OnPadderVisibilityChanged(bool isTopPadder, bool isVisible)
        {
            EdgeVisibilityChanged?.Invoke(isTopPadder, isVisible);
        }

        void OnHeaderToggle(object sectionKey)
        {
            if (_source is IExpansionToggle toggle)
            {
                // Capture the tapped header's on-screen offset BEFORE the toggle mutates the data, so the
                // rebuild can pin it back in place (see Rebuild's section-anchor branch).
                int hIdx = FindHeaderRowIndex(sectionKey);
                if (hIdx >= 0)
                {
                    _sectionAnchorDelta = _scroller.ScrollPosition
                                          - _scroller.GetScrollPositionForDataIndex(hIdx, EnhancedScroller.CellViewPositionEnum.Before);
                    _anchorSectionKey = sectionKey;
                    _hasSectionAnchor = true;
                }
                toggle.ToggleSection(sectionKey);
            }
        }

        // Index of the section-header row for a section key in the current snapshot, or -1 if absent.
        int FindHeaderRowIndex(object sectionKey)
        {
            var rows = _snapshot.Rows;
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].Kind == RowKind.SectionHeader && Equals(rows[i].SectionKey, sectionKey))
                {
                    return i;
                }
            }
            return -1;
        }

        void HandleSelected(ICollectionItem item)
        {
            _onItemSelected(item);
            if (_selection.Select(item.IdentityKey))
            {
                RefreshVisible();
            }
        }

        CellBindContext BuildContext(ICollectionItem item)
        {
            IAsyncCellContentLoader loader = _hasContentLoader ? _contentLoader : null;
            return new CellBindContext(loader, null, _selection.IsSelected(item.IdentityKey), HandleSelected, _textResolver);
        }

        int ComputeColumns()
        {
            if (_config.UseFixedColumns)
            {
                return _config.FixedColumns;
            }
            float width = _viewport.rect.width;
            return GridLayoutMath.Compute(width, _config.MinItemWidth, _config.Spacing, _config.DesignItemWidth).Columns;
        }

        void UpdateEmptyState()
        {
            if (!_useEmptyStateView)
            {
                return;
            }
            _emptyStateView.SetActive(_config.ShowEmptyState && _snapshot.IsEmpty);
        }

        GameObject ResolveItemCellPrefab(string templateKey)
        {
            bool found = _config.TryGetItemCellTemplate(templateKey, out GameObject prefab);
            Debug.Assert(found, $"{name}: no item-cell template registered for key '{templateKey}'.", this);
            return prefab;
        }

        EnhancedScrollerCellView ResolveRowPrefabCell(string templateKey)
        {
            bool found = _config.TryGetRowTemplate(templateKey, out GameObject prefab);
            Debug.Assert(found, $"{name}: no row template registered for key '{templateKey}'.", this);
            return prefab.GetComponent<EnhancedScrollerCellView>();
        }
    }
}
