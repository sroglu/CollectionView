using System.Threading;
using UnityEngine;

namespace PFound.CollectionView.Async
{
    /// <summary>
    /// Non-generic marker so a loosely-typed loader can travel through the non-generic cell contract
    /// and be cast to its concrete generic shape inside the cell.
    /// </summary>
    public interface IAsyncCellContentLoader
    {
    }

    /// <summary>
    /// Per-cell lazy content hydration for an already-visible cell (thumbnail, computed payload).
    /// Portable: the core depends only on Unity 6 native <see cref="Awaitable{T}"/> + a
    /// <see cref="CancellationToken"/>. A Playnest-side adapter implements this over AssetSystem/UniTask.
    /// This is NOT list-level paging - that is an out-of-scope future extension.
    /// </summary>
    public interface IAsyncCellContentLoader<in TItem, TResult> : IAsyncCellContentLoader
    {
        Awaitable<TResult> LoadAsync(TItem item, CancellationToken ct);
    }
}
