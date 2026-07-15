using System;
using System.Collections.Generic;
using System.Threading;
using EnhancedUI.EnhancedScroller;
using PFound.CollectionView.Cells;
using PFound.CollectionView.Items;
using PFound.CollectionView.Snapshot;
using UnityEngine;

namespace PFound.CollectionView.View
{
    /// <summary>
    /// A single recycled scroller row that hosts up to <c>columns</c> pooled item-cells - the grid trick
    /// the brief requires to stay fully hidden from callers. It is the scroller's cell; the item-cells are
    /// pooled children. Thin Unity shell: it owns intra-prefab refs and forwards per-cell cancellation.
    /// </summary>
    public sealed class RowCellView : EnhancedScrollerCellView
    {
        [SerializeField] RectTransform _itemContainer;

        readonly List<ICollectionCellView> _active = new List<ICollectionCellView>();
        readonly List<CancellationTokenSource> _tokens = new List<CancellationTokenSource>();

        ItemCellPool _pool;
        bool _bound;

        /// <summary>Container that holds the item-cells (assign a Horizontal/Grid layout on the prefab).</summary>
        public RectTransform ItemContainer => _itemContainer;

        /// <summary>Binds the row's members into pooled item-cells, each with its own per-cell token.</summary>
        public void BindRow(RowDescriptor row, ItemCellPool pool, Func<ICollectionItem, CellBindContext> contextFactory)
        {
            Debug.Assert(_itemContainer, "RowCellView: _itemContainer must be wired on the prefab.", this);
            RecycleRow();
            _pool = pool;
            _bound = true;

            var items = row.Items;
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var cell = pool.Rent(ResolveItemTemplate(item, row.TemplateKey), _itemContainer);
                var cts = new CancellationTokenSource();
                cell.Bind(item, contextFactory(item), cts.Token);
                _active.Add(cell);
                _tokens.Add(cts);
            }
        }

        /// <summary>
        /// Feature C: re-binds the already-hosted item-cells in place with fresh contexts (selection/state
        /// change) without renting/returning or disturbing scroll. Reuses the existing per-cell tokens.
        /// </summary>
        public void RefreshRow(RowDescriptor row, Func<ICollectionItem, CellBindContext> contextFactory)
        {
            if (!_bound)
            {
                return;
            }
            var items = row.Items;
            int n = Math.Min(items.Count, _active.Count);
            for (int i = 0; i < n; i++)
            {
                _active[i].Bind(items[i], contextFactory(items[i]), _tokens[i].Token);
            }
        }

        /// <summary>Returns all hosted item-cells to the pool and cancels their loads.</summary>
        public void RecycleRow()
        {
            for (int i = 0; i < _active.Count; i++)
            {
                _tokens[i].Cancel();
                _tokens[i].Dispose();
                _pool.Return(_active[i]);
            }
            _active.Clear();
            _tokens.Clear();
            _bound = false;
        }

        static string ResolveItemTemplate(ICollectionItem item, string rowTemplateKey)
        {
            if (item is ICellState stated)
            {
                object token = stated.StateToken;
                if (token is string s)
                {
                    return s;
                }
            }
            return rowTemplateKey;
        }
    }
}
