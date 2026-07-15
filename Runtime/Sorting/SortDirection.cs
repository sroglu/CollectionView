namespace PFound.CollectionView.Sorting
{
    /// <summary>
    /// Direction axis, orthogonal to the composite sort keys. Applied AFTER the composite
    /// comparison as a whole-order reversal, so it can be flipped independently of the primary key.
    /// </summary>
    public enum SortDirection
    {
        Ascending = 0,
        Descending = 1
    }
}
