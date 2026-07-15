# PFound.CollectionView

A generic, reusable, **virtualized** scrollable list/grid view for Unity 6, built on the
EnhancedScroller plugin. Flat lists and grouped (sectioned) lists are the **same code path** —
a flat list is just "one implicit section, zero headers".

- **Dependencies:** EnhancedScroller (`EnhancedUI` asmdef), Unity engine, TMPro. Nothing else —
  the module is liftable into any project. Async uses Unity 6 native `Awaitable<T>` +
  `CancellationToken` (no UniTask/AssetSystem in the core).
- **Portability note:** the runtime has **no Odin dependency**. Bool-gated optional config fields use a
  plain bool + `OnValidate` assert, and the Editor inspector greys disabled slots via
  `EditorGUI.DisabledScope` (the same wire-time contract Odin `[EnableIf]` would give).

## Architecture at a glance

```
Items/      role interfaces an item opts into: ICollectionItem (required) + IOrderable /
            ICellState / IDisplayContent / IGroupable (optional)
Model/      CollectionModel<T> (POCO): owns items, sort selection, filter pipeline, sections,
            expansion. Produces an immutable CollectionSnapshot. Flat = grouping off.
Snapshot/   CollectionSnapshot + RowDescriptor (union: SectionHeader | ItemRow | SectionEmpty) +
            SnapshotFlattener.Flatten — a single readable linear pass, NO running-offset index math.
Sorting/    ISortKey<T>, CompositeComparer<T> (primary → tie-breakers, orthogonal direction),
            SortMode<T>, SortRegistry<T>, SortCache<T>, ISectionComparer.
Filtering/  FilterPipeline<T> (AND + OR groups) + IdentityExclusionFilter<T>.
Async/      IAsyncCellContentLoader<TItem,TResult> + CellContentHydrator (per-cell CTS).
Cells/      ICollectionCellView (Bind / ApplyState / Unbind) + CellBindContext + section-cell contracts.
View/       CollectionScrollView (MonoBehaviour, IEnhancedScrollerDelegate) + RowCellView (hidden grid
            row) + GridLayoutMath + ItemCellPool + SelectionController.
Config/     CollectionViewConfig (ScriptableObject) + [SerializeReference] strategy slots.
```

### The materialized snapshot

The model materializes an immutable flat `CollectionSnapshot` once per structural change; the scroller
delegate just indexes into it (`Count = Rows.Count`, `Size(i) = Rows[i].Height`, cell = pool by
`Rows[i].TemplateKey`). Grouping, ordering and filtering compose in one readable `Flatten` pass:

1. partition items into sections by section key (declared taxonomy keys seed empty buckets),
2. per section: **filter** → **sort** (intra-section composite) → chunk into item rows,
3. order the sections via an `ISectionComparer` (independent of the item order),
4. emit per ordered section: header → (skip members if collapsed) → item rows | inline empty.

`EmptySectionPolicy` distinguishes *structurally empty* (declared, no items) from *filtered-to-empty*:
`Hide` (default) / `ShowIfFilteredEmpty` / `ShowPlaceholder`.

## Minimal usage

```csharp
using PFound.CollectionView.Model;
using PFound.CollectionView.Sorting;
using PFound.CollectionView.View;

// 1. Your item implements the roles it needs.
sealed class SaveSlot : ICollectionItem, IOrderable
{
    public SaveSlot(string id, long savedAt) { IdentityKey = id; SavedAt = savedAt; }
    public object IdentityKey { get; }
    public long SavedAt { get; }
    public System.IComparable GetOrderValue(string keyId) => SavedAt;
}

// 2. Build a model (POCO — no scene needed), register sort + filter.
var model = new CollectionModel<SaveSlot>();
model.SortRegistry.Register(new SortMode<SaveSlot>(
    "recent", new[] { new OrderableSortKey<SaveSlot>("date") }));
model.SetSortMode("recent", SortDirection.Descending);
model.Filter.Add(slot => slot.SavedAt > 0);
model.SetItems(LoadSlots());

// 3. Hand the model to the view (a wired CollectionScrollView prefab).
//    textResolver resolves opaque label keys → the module ships no literal UI text.
collectionScrollView.Bind(
    source: model,
    textResolver: key => Localize(key),
    onItemSelected: item => OpenSlot((SaveSlot)item));

// 4. Later: mutate the model; the view rebuilds and restores scroll automatically.
model.Filter.Clear();
collectionScrollView.ScrollTo(someSlot.IdentityKey);
```

### Grouping

Set `CollectionModel.Grouping = true`, provide `SetSectionKeySelector`, optionally
`SetSectionHeaderProvider` and `SetSectionComparer`. Header rows bind to a host prefab implementing
`ISectionHeaderCellView` (tap → collapse/expand); empty sections bind `ISectionEmptyCellView`.

### Async cell content

Implement `IAsyncCellContentLoader<TItem, TResult>` (returns `Awaitable<TResult>`), reference it from
the config's content-loader strategy slot, and drive it from your cell with a `CellContentHydrator` — it
owns the per-cell `CancellationToken` and cancels the in-flight load when the cell is recycled (`Unbind`).

## Testing

`CollectionModel<T>`, `CompositeComparer<T>`, `FilterPipeline<T>` and `SnapshotFlattener` are pure POCOs
covered by EditMode tests (`Tests/`) with fake items — no scene required.
