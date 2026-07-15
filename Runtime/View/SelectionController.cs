using System;
using PFound.CollectionView.Config;

namespace PFound.CollectionView.View
{
    /// <summary>
    /// Centralized single-active-selection state (feature D). Holds at most one selected identity and
    /// raises a change with (previous, current) so the view can refresh only the two affected cells in
    /// place. Pure POCO. In <see cref="SelectionMode.None"/> it never retains a highlight.
    /// </summary>
    public sealed class SelectionController
    {
        readonly SelectionMode _mode;
        bool _hasSelection;
        object _selectedIdentity;

        /// <summary>Raised on selection change: (previousIdentity, currentIdentity). Either may be null.</summary>
        public event Action<object, object> SelectionChanged;

        public SelectionController(SelectionMode mode)
        {
            _mode = mode;
        }

        public bool HasSelection => _hasSelection;

        public object SelectedIdentity => _selectedIdentity;

        public bool IsSelected(object identityKey) => _hasSelection && Equals(_selectedIdentity, identityKey);

        /// <summary>Selects an identity (Single mode). No-op highlight in None mode. Returns true if the highlight changed.</summary>
        public bool Select(object identityKey)
        {
            if (_mode == SelectionMode.None)
            {
                return false;
            }
            if (_hasSelection && Equals(_selectedIdentity, identityKey))
            {
                return false;
            }
            object previous = _hasSelection ? _selectedIdentity : null;
            _selectedIdentity = identityKey;
            _hasSelection = true;
            SelectionChanged?.Invoke(previous, identityKey);
            return true;
        }

        public void Clear()
        {
            if (!_hasSelection)
            {
                return;
            }
            object previous = _selectedIdentity;
            _selectedIdentity = null;
            _hasSelection = false;
            SelectionChanged?.Invoke(previous, null);
        }
    }
}
