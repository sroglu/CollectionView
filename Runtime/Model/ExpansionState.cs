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
        DefaultExpansion _default = DefaultExpansion.AllExpanded;

        public void SetDefault(DefaultExpansion defaultExpansion)
        {
            _default = defaultExpansion;
        }

        public bool IsCollapsed(object sectionKey)
        {
            if (_collapsed.TryGetValue(sectionKey, out bool collapsed))
            {
                return collapsed;
            }
            return _default == DefaultExpansion.AllCollapsed;
        }

        public void SetCollapsed(object sectionKey, bool collapsed)
        {
            _collapsed[sectionKey] = collapsed;
        }

        public void Toggle(object sectionKey)
        {
            _collapsed[sectionKey] = !IsCollapsed(sectionKey);
        }

        public void Clear()
        {
            _collapsed.Clear();
        }
    }
}
