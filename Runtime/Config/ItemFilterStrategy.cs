using System;
using PFound.CollectionView.Items;

namespace PFound.CollectionView.Config
{
    /// <summary>
    /// Data-authored include-predicate over <see cref="ICollectionItem"/> for <c>[SerializeReference]</c>
    /// filter slots on a config. Returns true when the item should remain visible. The type-safe path is
    /// still to add <see cref="Predicate{T}"/> in code to the model's <c>FilterPipeline</c>.
    /// </summary>
    [Serializable]
    public abstract class ItemFilterStrategy
    {
        public abstract bool Include(ICollectionItem item);

        /// <summary>Adapts this strategy to a <see cref="Predicate{T}"/> for the pipeline.</summary>
        public Predicate<ICollectionItem> AsPredicate() => Include;
    }
}
