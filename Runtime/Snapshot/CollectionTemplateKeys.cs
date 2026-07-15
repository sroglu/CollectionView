namespace PFound.CollectionView.Snapshot
{
    /// <summary>
    /// Default template keys the flattener stamps onto rows when the host does not override per row.
    /// These are internal identifiers (NOT user-facing text) used to pick a cell prefab from config.
    /// </summary>
    public static class CollectionTemplateKeys
    {
        public const string Header = "__header";
        public const string ItemRow = "__item_row";
        public const string Empty = "__empty";
    }
}
