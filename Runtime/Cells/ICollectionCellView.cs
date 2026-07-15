using System.Threading;
using PFound.CollectionView.Items;

namespace PFound.CollectionView.Cells
{
    /// <summary>
    /// Contract every item-cell (any template variation) implements. Cells are 100% self-contained:
    /// their Image/Label/badge refs are intra-prefab <c>[SerializeField]</c> wires; everything external
    /// arrives through <see cref="CellBindContext"/> at <see cref="Bind"/> time.
    /// </summary>
    public interface ICollectionCellView
    {
        /// <summary>
        /// Sets ALL visuals inline in one pass and kicks off any async content load. The
        /// <paramref name="cancellationToken"/> is the per-cell token - the load must observe it so a
        /// recycle cancels the in-flight work.
        /// </summary>
        void Bind(ICollectionItem item, in CellBindContext context, CancellationToken cancellationToken);

        /// <summary>Toggles sub-parts / swaps variant for the given state token (see <see cref="ICellState"/>).</summary>
        void ApplyState(object stateToken);

        /// <summary>Cancels any in-flight async load and returns to the pool clean.</summary>
        void Unbind();
    }
}
