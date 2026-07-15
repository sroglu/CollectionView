using System;
using PFound.CollectionView.Items;
using PFound.CollectionView.Sorting;

namespace PFound.CollectionView.Config
{
    /// <summary>
    /// Data-authored sort key over the base <see cref="ICollectionItem"/> abstraction, so it can be dropped
    /// into a <c>CollectionViewConfig</c> via <c>[SerializeReference]</c>. The primary, fully type-safe path
    /// is still to register <see cref="ISortKey{T}"/> in code on the model's registry; this exists for
    /// designer-driven screens that describe ordering in the asset.
    /// </summary>
    [Serializable]
    public abstract class SortKeyStrategy : ISortKey<ICollectionItem>
    {
        public abstract string Id { get; }

        public abstract int Compare(ICollectionItem a, ICollectionItem b);
    }
}
