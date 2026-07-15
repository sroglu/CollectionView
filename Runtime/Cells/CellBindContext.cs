using System;
using System.Collections.Generic;
using PFound.CollectionView.Async;
using PFound.CollectionView.Items;

namespace PFound.CollectionView.Cells
{
    /// <summary>
    /// Everything external a cell needs at <c>Bind</c> time, injected in one pass. Cells hold NO scene
    /// or service references through serialization - all outside state arrives here. Keeping this a
    /// single readonly struct means a config that references cell prefabs by asset changes no scene wiring.
    /// </summary>
    public readonly struct CellBindContext
    {
        /// <summary>Optional async content loader (marker; cast to the concrete generic type in the cell).</summary>
        public readonly IAsyncCellContentLoader ContentLoader;

        /// <summary>Host-set presentation flags (e.g. "new", "actionable") that alter look, not identity.</summary>
        public readonly IReadOnlyDictionary<string, bool> Flags;

        /// <summary>Whether this item is the single active selection (feature D).</summary>
        public readonly bool IsSelected;

        /// <summary>Invoked when the cell is tapped; meaning is host-defined.</summary>
        public readonly Action<ICollectionItem> OnSelected;

        /// <summary>
        /// Host localization callback: cell passes an opaque label key, host returns display text.
        /// The module itself ships no literal user-facing strings.
        /// </summary>
        public readonly Func<object, string> TextResolver;

        public CellBindContext(
            IAsyncCellContentLoader contentLoader,
            IReadOnlyDictionary<string, bool> flags,
            bool isSelected,
            Action<ICollectionItem> onSelected,
            Func<object, string> textResolver)
        {
            ContentLoader = contentLoader;
            Flags = flags;
            IsSelected = isSelected;
            OnSelected = onSelected;
            TextResolver = textResolver;
        }

        /// <summary>True when a named host flag is present and set.</summary>
        public bool HasFlag(string key)
        {
            var flags = Flags;
            return flags != null && flags.TryGetValue(key, out bool v) && v;
        }
    }
}
