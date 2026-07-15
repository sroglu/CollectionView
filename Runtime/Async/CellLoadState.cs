namespace PFound.CollectionView.Async
{
    /// <summary>Per-cell async content load state, so a cell can show a placeholder / spinner / fallback.</summary>
    public enum CellLoadState
    {
        Idle = 0,
        Loading = 1,
        Loaded = 2,
        Failed = 3
    }
}
