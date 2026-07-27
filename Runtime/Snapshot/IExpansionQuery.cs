namespace PFound.CollectionView.Snapshot
{
    /// <summary>Read-only expansion lookup the flattener consults to decide whether to emit a section's members.</summary>
    public interface IExpansionQuery
    {
        /// <summary>True when the section is collapsed (header still shown, members omitted).</summary>
        bool IsCollapsed(object sectionKey);

        /// <summary>False when the section is pinned open — the user cannot collapse it and Toggle is a no-op.
        /// Header cells use this to hide/disable their collapse affordance. Default is true (collapsible).</summary>
        bool IsCollapsible(object sectionKey);
    }
}
