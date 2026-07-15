using System;
using PFound.CollectionView.Async;
using PFound.CollectionView.Items;

namespace PFound.CollectionView.Config
{
    /// <summary>
    /// Data-authored content-loader slot for <c>[SerializeReference]</c> on a config. Supplies the async
    /// loader the cells use (or a handle to a scene/asset provider that returns one at runtime). Kept
    /// non-generic (result typed as <see cref="UnityEngine.Object"/>) so it serializes on the non-generic
    /// config; the fully-typed generic path stays available in code.
    /// </summary>
    [Serializable]
    public abstract class CellContentLoaderStrategy
    {
        /// <summary>Returns the loader instance for the cells. Called once at bind/config time.</summary>
        public abstract IAsyncCellContentLoader<IDisplayContent, UnityEngine.Object> Resolve();
    }
}
