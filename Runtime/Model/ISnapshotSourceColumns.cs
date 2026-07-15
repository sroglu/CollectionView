namespace PFound.CollectionView.Model
{
    /// <summary>
    /// Optional column sink implemented by responsive models. The view computes items-per-row from the
    /// container width and pushes it here before building the snapshot, so chunking stays a model concern.
    /// </summary>
    public interface IColumnSink
    {
        void SetColumns(int columns);
    }
}
