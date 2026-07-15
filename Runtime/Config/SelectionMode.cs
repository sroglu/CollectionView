namespace PFound.CollectionView.Config
{
    /// <summary>How selection behaves for a collection view.</summary>
    public enum SelectionMode
    {
        /// <summary>Tapping reports the item but no highlight state is retained.</summary>
        None = 0,

        /// <summary>At most one item is selected; selecting another clears the previous highlight.</summary>
        Single = 1
    }
}
