namespace PFound.CollectionView.Items
{
    /// <summary>
    /// Optional role for items that carry an async-loadable content payload
    /// (thumbnail, computed data). Exposes the opaque key an
    /// <see cref="Async.IAsyncCellContentLoader{TItem,TResult}"/> needs to hydrate the cell.
    /// </summary>
    /// <remarks>
    /// Synchronous visuals (labels, static icons, badges) are read by the concrete cell
    /// directly from the item in <c>Bind</c>; this role only concerns the async payload key.
    /// </remarks>
    public interface IDisplayContent
    {
        /// <summary>Opaque key handed to the async content loader (e.g. an asset address).</summary>
        object ContentKey { get; }
    }
}
