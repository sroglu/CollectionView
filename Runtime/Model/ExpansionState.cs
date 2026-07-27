using System.Collections.Generic;
using PFound.CollectionView.Config;
using PFound.CollectionView.Snapshot;

namespace PFound.CollectionView.Model
{
    /// <summary>
    /// Mutable per-section collapsed/expanded state. Pure POCO consulted by the flattener via
    /// <see cref="IExpansionQuery"/>. A section absent from the map takes the configured default.
    /// </summary>
    public sealed class ExpansionState : IExpansionQuery
    {
        readonly Dictionary<object, bool> _collapsed = new Dictionary<object, bool>();
        readonly HashSet<object> _pinnedOpen = new HashSet<object>();
        DefaultExpansion _default = DefaultExpansion.AllExpanded;

        public void SetDefault(DefaultExpansion defaultExpansion)
        {
            _default = defaultExpansion;
        }

        /// <summary>
        /// Pin a section open (<paramref name="collapsible"/> = false): the user can't collapse it, <see cref="Toggle"/>
        /// and <see cref="SetCollapsed"/> become no-ops for it, and it always reports expanded. Passing true restores
        /// normal (collapsible) behaviour. This is section CONFIG, not runtime state — <see cref="Clear"/> keeps it.
        /// </summary>
        public void SetCollapsible(object sectionKey, bool collapsible)
        {
            if (collapsible)
            {
                _pinnedOpen.Remove(sectionKey);
            }
            else
            {
                _pinnedOpen.Add(sectionKey);
                _collapsed.Remove(sectionKey); // drop any prior collapsed state so it's forced open
            }
        }

        public bool IsCollapsible(object sectionKey) => !_pinnedOpen.Contains(sectionKey);

        public bool IsCollapsed(object sectionKey)
        {
            if (_pinnedOpen.Contains(sectionKey)) return false; // pinned open never collapses
            if (_collapsed.TryGetValue(sectionKey, out bool collapsed))
            {
                return collapsed;
            }
            return _default == DefaultExpansion.AllCollapsed;
        }

        public void SetCollapsed(object sectionKey, bool collapsed)
        {
            if (_pinnedOpen.Contains(sectionKey)) return; // pinned open: ignore
            _collapsed[sectionKey] = collapsed;
        }

        public void Toggle(object sectionKey)
        {
            if (_pinnedOpen.Contains(sectionKey)) return; // pinned open: no-op
            _collapsed[sectionKey] = !IsCollapsed(sectionKey);
        }

        public void Clear()
        {
            _collapsed.Clear(); // runtime collapse state only; the pinned-open config persists
        }
    }
}
