using System;
using System.Threading;
using UnityEngine;

namespace PFound.CollectionView.Async
{
    /// <summary>
    /// Manages a single cell's async content load: owns the per-cell <see cref="CancellationTokenSource"/>,
    /// tracks <see cref="State"/>, and cancels an in-flight load when the cell is rebound or recycled.
    /// Pure C# (no MonoBehaviour); the owning cell forwards results to its own visuals via the callbacks.
    /// </summary>
    public sealed class CellContentHydrator<TItem, TResult>
    {
        CancellationTokenSource _cts;
        bool _running;

        public CellLoadState State { get; private set; } = CellLoadState.Idle;

        /// <summary>
        /// Cancels any prior load, then starts hydrating from <paramref name="loader"/>. The internal source
        /// is linked to the view-supplied per-cell <paramref name="cellToken"/>, so either a recycle
        /// (<see cref="Cancel"/> / <c>Unbind</c>) or a view teardown cancels the in-flight load. On success
        /// invokes <paramref name="onLoaded"/>; on load failure invokes <paramref name="onFailed"/>; on
        /// cancellation stays silent.
        /// </summary>
        public void Begin(IAsyncCellContentLoader<TItem, TResult> loader, TItem item, CancellationToken cellToken,
            Action<TResult> onLoaded, Action onFailed)
        {
            Cancel();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cellToken);
            _running = true;
            State = CellLoadState.Loading;
            Run(loader, item, _cts.Token, onLoaded, onFailed);
        }

        /// <summary>Cancels the in-flight load (if any) and returns to <see cref="CellLoadState.Idle"/>.</summary>
        public void Cancel()
        {
            if (!_running)
            {
                return;
            }
            _cts.Cancel();
            _cts.Dispose();
            _running = false;
            State = CellLoadState.Idle;
        }

        async void Run(IAsyncCellContentLoader<TItem, TResult> loader, TItem item, CancellationToken ct,
            Action<TResult> onLoaded, Action onFailed)
        {
            // Async boundary: cancellation (recycle) and asset-load failure are the only throwables we
            // translate here - the same external/IO/async boundary CODING-STYLE Sec 3 permits.
            try
            {
                TResult result = await loader.LoadAsync(item, ct);
                if (ct.IsCancellationRequested)
                {
                    return;
                }
                State = CellLoadState.Loaded;
                onLoaded(result);
            }
            catch (OperationCanceledException)
            {
                // Cell was rebound/recycled mid-load - expected, drop the result silently.
            }
            catch (Exception e)
            {
                if (ct.IsCancellationRequested)
                {
                    return;
                }
                State = CellLoadState.Failed;
                onFailed();
                Debug.LogException(e);
            }
        }
    }
}
