using System.Collections.Generic;

namespace PFound.CollectionView.Sorting
{
    /// <summary>
    /// A named ordered chain of <see cref="ISortKey{T}"/> (primary first, then tie-breakers).
    /// Data-driven: callers register their own modes and chains - the core hardcodes none.
    /// </summary>
    public sealed class SortMode<T>
    {
        /// <summary>Stable id used to select this mode (and to key its sort cache).</summary>
        public string Id { get; }

        /// <summary>
        /// Optional host-supplied display-label key. The module never renders literal text;
        /// a chrome dropdown resolves this key through the host's localization callback.
        /// </summary>
        public string LabelKey { get; }

        /// <summary>Ordered keys: [0] is primary, the rest are tie-breakers in order.</summary>
        public IReadOnlyList<ISortKey<T>> Keys { get; }

        public SortMode(string id, IReadOnlyList<ISortKey<T>> keys, string labelKey = null)
        {
            Id = id;
            Keys = keys;
            LabelKey = labelKey;
        }
    }
}
