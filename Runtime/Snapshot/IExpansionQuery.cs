namespace PFound.CollectionView.Snapshot
{
    /// <summary>Read-only expansion lookup the flattener consults to decide whether to emit a section's members.</summary>
    public interface IExpansionQuery
    {
        /// <summary>True when the section is collapsed (header still shown, members omitted).</summary>
        bool IsCollapsed(object sectionKey);
    }
}
