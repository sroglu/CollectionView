namespace PFound.CollectionView.Model
{
    /// <summary>Optional capability implemented by grouped models so a header tap can collapse/expand a section.</summary>
    public interface IExpansionToggle
    {
        void ToggleSection(object sectionKey);
    }
}
