namespace PFound.CollectionView.Items
{
    /// <summary>
    /// Required role for anything that can appear in a <c>CollectionScrollView</c>.
    /// Exposes a stable, opaque identity used for filter membership, exclusion,
    /// and scroll-to-item resolution. The key MUST persist across reorders and
    /// refreshes so the same logical item keeps the same identity between snapshots.
    /// </summary>
    /// <remarks>
    /// The key is treated as opaque: the model only uses <see cref="object.Equals(object)"/>
    /// and <see cref="object.GetHashCode"/> on it (via a dictionary). A <see cref="string"/>,
    /// a value-type id, or a domain key object are all valid - choose whatever is cheap and stable.
    /// </remarks>
    public interface ICollectionItem
    {
        /// <summary>Stable, opaque identity. Never changes for the lifetime of the logical item.</summary>
        object IdentityKey { get; }
    }
}
