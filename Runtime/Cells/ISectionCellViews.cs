using System;
using PFound.CollectionView.Snapshot;

namespace PFound.CollectionView.Cells
{
    /// <summary>
    /// Contract for a section-header row cell (host prefab). Renders the rollup; any label comes from the
    /// host <paramref name="textResolver"/> - the module ships no literal text. Tapping toggles collapse
    /// via <paramref name="onToggle"/>.
    /// </summary>
    public interface ISectionHeaderCellView
    {
        void BindHeader(SectionHeaderData data, Func<object, string> textResolver, Action<object> onToggle);
    }

    /// <summary>Contract for an inline empty-section placeholder row cell (host prefab).</summary>
    public interface ISectionEmptyCellView
    {
        void BindEmpty(object sectionKey, Func<object, string> textResolver);
    }
}
