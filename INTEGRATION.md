# PFound.CollectionView — Integration Playbook (installer)

> ## ⚑ AGENT DIRECTIVE — read this first
>
> If a user asked you to **integrate / add / use PFound.CollectionView** in some screen or list, do
> **NOT** implement it inline in the main conversation. **Dispatch a sub-agent** and hand it this file
> (`Assets/PFound/CollectionView/INTEGRATION.md`) as its instructions. The sub-agent runs this entire
> playbook in an isolated context:
> 1. Collect the **§1 requirements**. Any answer you don't already know from the existing UI/spec is a
>    **question to ask the user — never guess.**
> 2. Perform **§2 → §5** (asmdef refs, code, assets, wiring).
> 3. **Self-verify with §6**; obey the §7 pitfalls. Do not report "done" until every §6 check passes.
>
> Relay only the sub-agent's final summary back to the user. Reason for the sub-agent: this is a
> multi-file, multi-step migration — isolating it keeps the main conversation clean and the steps
> reproducible.

---

Executable, standardized recipe for wiring this module into a screen/list. This file is the
*procedure*; read `Runtime/README.md` for the API/architecture reference.

Canonical worked example in this repo: the Camping lobby env picker
(`Assets/MiniGames/CampingAdventure/Packing/EnvList/*` + `Assets/GameSpecific/CollectionView/EnvListConfig.asset`
+ `Assets/MiniGames/CampingAdventure/Prefabs/EnvList/*`). Copy-adapt it rather than authoring from zero.

---

## 0. When to use

Use this when a screen shows a **data-driven list or grid** whose contents come from a catalog/save/service
and should update as data changes (items added, unlocked, filtered, reordered). Do **not** hand-place tiles
in a container and `Instantiate` per item — that is exactly what this module replaces (pooling, grouping,
ordering, async content, scroll-preservation come for free).

---

## 1. Requirements — ASK before writing code

Gather these first. Each answer selects a branch below. If integrating for a user, ask them; if a value has
an obvious default from the existing (pre-migration) UI, state the assumption and proceed.

1. **Source & identity** — what is the data source (catalog SO / save / service)? What is each item's
   **stable identity key** (never changes across refresh/reorder)?
2. **Grouping** — one flat list, or sections? If sections: what is the **section key** per item, the
   **section order**, and the **empty-section policy** (`Hide` / `ShowIfFilteredEmpty` / `ShowPlaceholder`)?
3. **Ordering** — within a section, is there a sort (by date/name/…)? Direction? Or keep source order?
4. **Async content** — do cells load remote/heavy content (thumbnails, bundles)? If yes, you need an
   `IAsyncCellContentLoader` + a `CellContentHydrator` in the cell. If the icon is a direct `Sprite` ref on
   the item, skip async entirely.
5. **Layout** — single column, or a fixed **N-column grid**?
6. **Selection** — none / single / multi? What does a tap do?
7. **Disabled items** — are some items locked/non-selectable (greyed, no action)?
8. **Collapsible headers** — (grouped only) can the user tap a header to collapse/expand its section?

Write the answers down; they parameterize every step.

---

## 2. Prerequisites — asmdef references

Add to the **host** asmdef `references[]` (the assembly that will hold the item/cell/model code):

- `PFound.CollectionView` — always.
- `EnhancedUI` — always (the view is an `IEnhancedScrollerDelegate`; cells/rows extend EnhancedScroller types).
- `Unity.TextMeshPro` — if cells show TMP text.
- Your content-loader deps (e.g. `PFound.ContentDelivery`, `UniTask`) — **only if** answer 4 = async.

Asmdef refs are **not transitive** — list every assembly whose types you touch directly.

---

## 3. Code (host assembly)

Namespaces: `PFound.CollectionView.{Items,Model,Snapshot,Sorting,Config,Cells,Async,View}`.

### 3.1 Item type — `ICollectionItem` (+ optional roles)

One POCO per logical row-item. Implement only the roles the requirements need:
`IGroupable` (grouping), `IOrderable` (sort), `IDisplayContent` (async content key).

```csharp
public sealed class <Xxx>Item : ICollectionItem   // + IGroupable, IOrderable, IDisplayContent as needed
{
    public <Domain> Source { get; }               // keep the ref you need at tap time
    public bool Locked { get; }                   // if requirement 7
    // display fields: Label, Icon (Sprite) or content address, Color, ...

    public <Xxx>Item(...) { ... }

    object ICollectionItem.IdentityKey => Source.Id;          // STABLE, opaque
    // object IGroupable.SectionKey => Locked ? "locked" : "available";   // if grouped
    // int IGroupable.IntraSectionOrder => 0;                            // sort uses the registered key
    // IComparable IOrderable.GetOrderValue(string keyId) => SortValue;  // if sorted
    // object IDisplayContent.ContentKey => ContentAddress;             // if async
}
```

### 3.2 Item factory — source → items

Pure, testable. Joins catalog with per-profile/service state.

```csharp
public static class <Xxx>ItemFactory
{
    public static List<<Xxx>Item> Build(<Source> source, Func<<Domain>, string> labelResolver) { ... }
}
```
Keep localization/service lookups out of the factory — pass resolvers in (host owns the strings).

### 3.3 Model builder — `CollectionModel<T>`

```csharp
public static class <Xxx>ListModelBuilder
{
    public static CollectionModel<<Xxx>Item> Build(IEnumerable<<Xxx>Item> items)
    {
        var settings = new FlattenSettings {
            Grouping = <true/false>,                       // requirement 2
            EmptyPolicy = EmptySectionPolicy.Hide,          // requirement 2
        };
        var model = new CollectionModel<<Xxx>Item>(settings);

        // --- grouped only ---
        model.SetSectionKeySelector(i => i.<sectionKey>);
        model.SetSectionComparer(new ExplicitSectionOrderComparer(new object[]{ "available", "locked" }));

        // --- sorted only ---
        model.SortRegistry.Register(new SortMode<<Xxx>Item>("<mode>",
            new ISortKey<<Xxx>Item>[]{ new DelegateSortKey<<Xxx>Item>("<key>", i => i.<value>) }));

        model.Expansion.SetDefault(DefaultExpansion.AllExpanded);   // grouped
        model.SetItems(items);
        model.SetSortMode("<mode>", SortDirection.Descending);      // sorted
        return model;
    }
}
```
Flat list = `Grouping = false`, no selector/comparer, keep source order (skip the sort block).

### 3.4 Cell view — `ICollectionCellView` (self-contained)

The cell holds ONLY intra-prefab `[SerializeField]` wires; every external thing arrives via
`CellBindContext`. **Include the two pooling-hygiene items** (see §7):

```csharp
public sealed class <Xxx>Cell : MonoBehaviour, ICollectionCellView
{
    [SerializeField] Image _background; [SerializeField] Image _icon;
    [SerializeField] TMP_Text _label;   [SerializeField] GameObject _lockOverlay;
    [SerializeField] Button _button;

    <Xxx>Item _item; Action<ICollectionItem> _onSelected;

    void Awake() { Debug.Assert(_button, $"{name}: refs", this); _button.onClick.AddListener(OnClicked); }

    public void Bind(ICollectionItem item, in CellBindContext ctx, CancellationToken ct)
    {
        _item = (<Xxx>Item)item; _onSelected = ctx.OnSelected;
        _label.text = _item.Label; _icon.sprite = _item.Icon; _icon.enabled = _item.Icon != null;
        _background.color = _item.Color;
        if (_lockOverlay) _lockOverlay.SetActive(_item.Locked);
        _button.interactable = !_item.Locked;
        // async variant: _thumb.enabled=false; _hydrator.Begin(loader, _item, ct, OnLoaded, OnFailed);
        CancelButtonTransition();                              // §7 — pooled reuse hygiene
    }
    public void ApplyState(object s) { }
    public void Unbind() { /* async: _hydrator.Cancel(); clear content */ }

    void OnClicked() { if (_item.Locked) return; _onSelected(_item); }

    // Snap the button's Selectable to its state instantly so a pooled GO reused from a
    // disabled item to an enabled one does not bleed the previous item's ColorTint fade.
    void CancelButtonTransition() { _button.enabled = false; _button.enabled = true; }
}
```
Grouped lists also need a header cell implementing `ISectionHeaderCellView` (tap → toggle) — copy
`EnvSectionHeaderCell.cs`.

---

## 4. Assets

### 4.1 Prefabs

Copy the env-list prefabs and re-point scripts. Hard requirements:

- **Item cell prefab** (`<Xxx>Cell`): the `[SerializeField]` wires; `Button.Navigation = None` (§7);
  a `LayoutElement` whose `preferredHeight`/`preferredWidth` define the cell size — the view reads it
  (`GetCellViewSize` measures the row template prefab, NOT a magic number).
- **Row prefab** (`RowCellView` + `HorizontalLayoutGroup`, `childControl`+`forceExpandWidth`): hosts up to
  N item cells for a grid. Give it a `LayoutElement` height = the item-row height. **`cellIdentifier` MUST
  be unique** per template (row vs header vs empty) — a shared/empty id collides the EnhancedScroller pool
  and throws `InvalidCastException` on scroll.
- **Section header prefab** (grouped only): `ISectionHeaderCellView` + `Button` (Navigation=None) + bg + label;
  distinct `cellIdentifier`.

### 4.2 Config SO — `CollectionViewConfig`

`Assets > Create > PFound > CollectionView > Config` (or copy `EnvListConfig.asset`). Set: grouping flag,
empty policy, default expansion, selection mode, `useFixedColumns` + `fixedColumns` (grid),
`rowTemplates`/`itemCellTemplates` (template key → prefab), and `useContentLoader` + the content-loader
strategy slot (async only). Put it under `Assets/GameSpecific/CollectionView/`.

### 4.3 The `CollectionScrollView` GameObject (scene/prefab wiring)

Copy the env-list's `EnvScrollView` subtree. Structure + **the wiring gotcha**:

```
<ListRoot>  (ScrollRect + EnhancedScroller + CollectionScrollView + Mask + Image[raycast])
├─ Viewport        ← MUST equal <ListRoot> rect (see §7 viewport rule)
├─ PoolHidden      ← inactive; wired to CollectionScrollView._poolHiddenParent
└─ (Container)     ← EnhancedScroller creates this at runtime under <ListRoot>
```
Wire on `CollectionScrollView`: `_scroller`, `_config`, `_viewport`, `_poolHiddenParent`, optional empty-state.
The Mask lives on `<ListRoot>` (EnhancedScroller parents Container as a **sibling** of Viewport, so a mask on
Viewport would not clip it).

---

## 5. Bind from the host (screen/view)

```csharp
[SerializeField] CollectionScrollView _listView;   // wired in the prefab

var items    = <Xxx>ItemFactory.Build(source, ResolveLabel);
var model    = <Xxx>ListModelBuilder.Build(items);
// grouped: model.SetSectionHeaderProvider(k => HeaderLocKey(k));
_listView.Bind(model, textResolver: ResolveLoc, onItemSelected: item => OnSelected((<Xxx>Item)item));
```
Remove the old per-item `Instantiate` loop / tile container from the host.

---

## 6. Verify (self-check — a sub-agent MUST run these)

1. **Compile clean.** After any `.cs` edit, exit Play (Play mode *defers* compilation) and confirm
   `Library/ScriptAssemblies/<HostAsm>.dll` mtime > the `.cs` mtime before trusting a test.
2. **Model consistency:** in Play, `content.rect.height == scroller.ScrollSize + scrollRect.viewport.rect.height`.
   If not, the viewport is mis-sized (§7).
3. **Renders + scrolls:** cells appear, heights correct, scroll reaches the true bottom.
4. **Grouped:** collapse/expand a header — the tapped header stays put (no jump); expand keeps it pinned.
5. **Pooled reuse:** rapidly toggle/refresh — no stale highlight/fade bleeds onto a reused cell.

---

## 7. Pitfalls (hard-won — do not rediscover these)

- **Viewport size MUST equal the rect EnhancedScroller uses.** EnhancedScroller derives `ScrollSize` from the
  **ScrollRect GameObject's** rect, but `ScrollRect` does its scroll math against `ScrollRect.viewport`. If the
  Viewport child is inset (e.g. `offsetMin.y=24/offsetMax.y=-24`) the two disagree → the virtualization model
  drifts from the real content position: scroll drift, collapse jump, header off-screen. **Fix:** make the
  Viewport fill the ScrollRect GO (offsets 0), or point `ScrollRect.viewport` at the ScrollRect's own transform.
  Diagnose in one line: `contentH == ScrollSize + viewport.rect.height`.
- **Pooled-cell state bleed.** A pooled GameObject is reused for a *different* item each rebuild. Any animated
  transient state from the previous item must be cancelled **at the data change (Bind)**, not disabled globally.
  Concretely: bounce `Button.enabled` in `Bind` to snap the Selectable's ColorTint (keeps `fadeDuration` for
  real interaction); set cell `Button.Navigation = None` so a recycled *selected* header can't hop selection to
  a neighbour. Deferred async is safe **iff** the hydrator cancels on rebind and re-checks its token on completion.
- **Distinct `cellIdentifier` per template** — shared/empty ids collide the pool (`InvalidCastException`).
- **Row heights come from the prefab**, not the snapshot — the view measures the row template's
  `LayoutElement.preferredHeight`/rect. If rows render 1px tall, the template has no `LayoutElement` height.
- **Play mode defers script compilation** — filesystem `.cs` writes sit in the old DLL until Play exits.
  Verify DLL mtime, don't trust "nothing changed".
- **Measurement in Play:** compare two `RectTransform.position` values (a jump = `|y1-y0|`) — reliable. Mixing
  `rect.height` (unscaled canvas units) with `position` (scaled screen px) is NOT (Canvas-scale artifact).

---

## 8. Sub-agent execution notes

This playbook is self-contained: give the agent (a) the target screen/host + its current list impl, and
(b) the §1 answers. It then: adds §2 refs → writes §3 files → copies/adapts §4 assets → wires §5 → runs §6.
Any §1 answer left blank is a **question to surface**, not a guess. The agent must not report "done" until
every §6 check passes (it self-verifies via Play-mode inspection, per §7's measurement rule).
