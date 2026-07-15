using System;
using System.Collections.Generic;
using PFound.CollectionView.Items;

namespace PFound.CollectionView.Filtering
{
    /// <summary>
    /// Built-in exclusion-by-identity predicate. Items whose <see cref="ICollectionItem.IdentityKey"/>
    /// is in the excluded set are hidden. Exposed as a <see cref="Predicate{T}"/> so it plugs into a
    /// <see cref="FilterPipeline{T}"/> alongside attribute/state predicates.
    /// </summary>
    public sealed class IdentityExclusionFilter<T> where T : ICollectionItem
    {
        readonly HashSet<object> _excluded = new HashSet<object>();

        public event Action Changed;

        public int Count => _excluded.Count;

        public void Exclude(object identityKey)
        {
            if (_excluded.Add(identityKey))
            {
                Changed?.Invoke();
            }
        }

        public void Include(object identityKey)
        {
            if (_excluded.Remove(identityKey))
            {
                Changed?.Invoke();
            }
        }

        public void Clear()
        {
            if (_excluded.Count > 0)
            {
                _excluded.Clear();
                Changed?.Invoke();
            }
        }

        public bool IsExcluded(object identityKey) => _excluded.Contains(identityKey);

        /// <summary>Include-predicate: returns true when the item is NOT excluded.</summary>
        public Predicate<T> AsPredicate() => item => !_excluded.Contains(item.IdentityKey);
    }
}
