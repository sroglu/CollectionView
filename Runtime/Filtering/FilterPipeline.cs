using System;
using System.Collections.Generic;

namespace PFound.CollectionView.Filtering
{
    /// <summary>
    /// Composable item-level predicate pipeline. An item is visible when EVERY AND-predicate
    /// passes AND every non-empty OR-group has at least one passing predicate. Empty pipeline
    /// -> everything passes. Raises <see cref="Changed"/> on any structural mutation so the model
    /// can recompute the visible space.
    /// </summary>
    public sealed class FilterPipeline<T>
    {
        readonly List<Predicate<T>> _and = new List<Predicate<T>>();
        readonly List<OrGroup> _orGroups = new List<OrGroup>();

        /// <summary>Raised whenever a predicate or group is added, removed, or cleared.</summary>
        public event Action Changed;

        public int PredicateCount => _and.Count;
        public int OrGroupCount => _orGroups.Count;

        /// <summary>Adds an AND-combined predicate. Returns the same predicate for later removal.</summary>
        public Predicate<T> Add(Predicate<T> predicate)
        {
            _and.Add(predicate);
            RaiseChanged();
            return predicate;
        }

        public bool Remove(Predicate<T> predicate)
        {
            bool removed = _and.Remove(predicate);
            if (removed)
            {
                RaiseChanged();
            }
            return removed;
        }

        /// <summary>Creates and registers an OR-group. Adding members to it raises <see cref="Changed"/>.</summary>
        public OrGroup AddOrGroup()
        {
            var group = new OrGroup(RaiseChanged);
            _orGroups.Add(group);
            RaiseChanged();
            return group;
        }

        public bool RemoveOrGroup(OrGroup group)
        {
            bool removed = _orGroups.Remove(group);
            if (removed)
            {
                RaiseChanged();
            }
            return removed;
        }

        public void Clear()
        {
            bool had = _and.Count > 0 || _orGroups.Count > 0;
            _and.Clear();
            _orGroups.Clear();
            if (had)
            {
                RaiseChanged();
            }
        }

        /// <summary>True when the item survives the whole pipeline.</summary>
        public bool Matches(T item)
        {
            for (int i = 0; i < _and.Count; i++)
            {
                if (!_and[i](item))
                {
                    return false;
                }
            }

            for (int g = 0; g < _orGroups.Count; g++)
            {
                if (!_orGroups[g].Matches(item))
                {
                    return false;
                }
            }

            return true;
        }

        void RaiseChanged() => Changed?.Invoke();

        /// <summary>
        /// A disjunction of predicates: the group passes when ANY member passes.
        /// An empty group is treated as passing (it imposes no constraint).
        /// </summary>
        public sealed class OrGroup
        {
            readonly List<Predicate<T>> _predicates = new List<Predicate<T>>();
            readonly Action _notifyChanged;

            internal OrGroup(Action notifyChanged)
            {
                _notifyChanged = notifyChanged;
            }

            public int Count => _predicates.Count;

            public Predicate<T> Add(Predicate<T> predicate)
            {
                _predicates.Add(predicate);
                _notifyChanged();
                return predicate;
            }

            public bool Remove(Predicate<T> predicate)
            {
                bool removed = _predicates.Remove(predicate);
                if (removed)
                {
                    _notifyChanged();
                }
                return removed;
            }

            internal bool Matches(T item)
            {
                if (_predicates.Count == 0)
                {
                    return true;
                }
                for (int i = 0; i < _predicates.Count; i++)
                {
                    if (_predicates[i](item))
                    {
                        return true;
                    }
                }
                return false;
            }
        }
    }
}
