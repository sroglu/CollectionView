using System;
using System.Collections.Generic;
using PFound.CollectionView.Cells;
using UnityEngine;

namespace PFound.CollectionView.View
{
    /// <summary>
    /// Object pool for the item-cells that live inside an <see cref="RowCellView"/> row (the hidden grid
    /// trick). Cells are instantiated from a host-supplied prefab resolved per template key and reused.
    /// No runtime UI construction from scratch - every cell is an <c>Instantiate</c> of a template.
    /// </summary>
    public sealed class ItemCellPool
    {
        readonly Func<string, GameObject> _prefabResolver;
        readonly Transform _hiddenParent;
        readonly Dictionary<string, Stack<ICollectionCellView>> _free = new Dictionary<string, Stack<ICollectionCellView>>();
        readonly Dictionary<ICollectionCellView, string> _keyOf = new Dictionary<ICollectionCellView, string>();

        /// <param name="prefabResolver">Template key -> item-cell prefab (root carries an <see cref="ICollectionCellView"/>).</param>
        /// <param name="hiddenParent">Inactive parent that holds pooled (recycled) cells off-screen.</param>
        public ItemCellPool(Func<string, GameObject> prefabResolver, Transform hiddenParent)
        {
            _prefabResolver = prefabResolver;
            _hiddenParent = hiddenParent;
        }

        /// <summary>Rents a cell for the template key, parenting it under <paramref name="parent"/>.</summary>
        public ICollectionCellView Rent(string templateKey, Transform parent)
        {
            ICollectionCellView cell;
            if (_free.TryGetValue(templateKey, out var stack) && stack.Count > 0)
            {
                cell = stack.Pop();
            }
            else
            {
                GameObject prefab = _prefabResolver(templateKey);
                GameObject instance = UnityEngine.Object.Instantiate(prefab, parent);
                cell = instance.GetComponent<ICollectionCellView>();
                _keyOf[cell] = templateKey;
            }

            var t = ((Component)cell).transform;
            t.SetParent(parent, false);
            ((Component)cell).gameObject.SetActive(true);
            return cell;
        }

        /// <summary>Unbinds and returns a cell to its template pool.</summary>
        public void Return(ICollectionCellView cell)
        {
            cell.Unbind();
            string key = _keyOf[cell];
            var go = ((Component)cell).gameObject;
            go.SetActive(false);
            ((Component)cell).transform.SetParent(_hiddenParent, false);

            if (!_free.TryGetValue(key, out var stack))
            {
                stack = new Stack<ICollectionCellView>();
                _free[key] = stack;
            }
            stack.Push(cell);
        }
    }
}
