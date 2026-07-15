namespace PFound.CollectionView.Items
{
    /// <summary>
    /// Optional role for items whose visual variant depends on a small state token
    /// (e.g. ready / busy / done / empty). The mapping from token to template/sub-part
    /// is host-pluggable (see <c>CollectionViewConfig</c>) - the core never bakes a fixed enum.
    /// </summary>
    public interface ICellState
    {
        /// <summary>Opaque token selecting which visual variant renders this item.</summary>
        object StateToken { get; }
    }
}
