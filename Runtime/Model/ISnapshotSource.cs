using System;
using PFound.CollectionView.Snapshot;

namespace PFound.CollectionView.Model
{
    /// <summary>
    /// Non-generic surface the (non-generic) view consumes: a change signal plus the current immutable
    /// snapshot. Lets <c>CollectionScrollView</c> render any <c>CollectionModel&lt;T&gt;</c> without knowing T.
    /// </summary>
    public interface ISnapshotSource
    {
        event Action Changed;

        CollectionSnapshot BuildSnapshot();
    }
}
