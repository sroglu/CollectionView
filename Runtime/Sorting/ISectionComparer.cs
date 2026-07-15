using System;
using System.Collections.Generic;

namespace PFound.CollectionView.Sorting
{
    /// <summary>
    /// Snapshot of a section for section-level ordering (Sec 11e). Carries the section key,
    /// its header data, and the post-filter visible member count so a comparer can order
    /// sections by key, by a fixed/explicit order, or by an aggregate.
    /// </summary>
    public readonly struct SectionView
    {
        public readonly object Key;
        public readonly object HeaderData;
        public readonly int VisibleCount;

        public SectionView(object key, object headerData, int visibleCount)
        {
            Key = key;
            HeaderData = headerData;
            VisibleCount = visibleCount;
        }
    }

    /// <summary>
    /// Orders the sections themselves. Independent of the intra-section item order.
    /// </summary>
    public interface ISectionComparer : IComparer<SectionView>
    {
    }

    /// <summary>Orders sections by comparing their keys with the default comparer (keys must be comparable).</summary>
    public sealed class NaturalSectionComparer : ISectionComparer
    {
        public int Compare(SectionView a, SectionView b)
            => Comparer<object>.Default.Compare(a.Key, b.Key);
    }

    /// <summary>
    /// Orders sections by an explicit, fixed key order. Keys not present in the order list
    /// are placed after all known keys, preserving their relative encounter order.
    /// </summary>
    public sealed class ExplicitSectionOrderComparer : ISectionComparer
    {
        readonly Dictionary<object, int> _rank;

        public ExplicitSectionOrderComparer(IReadOnlyList<object> orderedKeys)
        {
            _rank = new Dictionary<object, int>(orderedKeys.Count);
            for (int i = 0; i < orderedKeys.Count; i++)
            {
                _rank[orderedKeys[i]] = i;
            }
        }

        public int Compare(SectionView a, SectionView b)
        {
            int ra = _rank.TryGetValue(a.Key, out int va) ? va : int.MaxValue;
            int rb = _rank.TryGetValue(b.Key, out int vb) ? vb : int.MaxValue;
            return ra.CompareTo(rb);
        }
    }

    /// <summary>Orders sections by their post-filter visible member count.</summary>
    public sealed class SectionCountComparer : ISectionComparer
    {
        readonly int _sign;

        public SectionCountComparer(SortDirection direction = SortDirection.Descending)
        {
            _sign = direction == SortDirection.Descending ? -1 : 1;
        }

        public int Compare(SectionView a, SectionView b)
            => _sign * a.VisibleCount.CompareTo(b.VisibleCount);
    }

    /// <summary>Orders sections by an arbitrary caller-supplied comparison of the section view.</summary>
    public sealed class DelegateSectionComparer : ISectionComparer
    {
        readonly Comparison<SectionView> _compare;

        public DelegateSectionComparer(Comparison<SectionView> compare)
        {
            _compare = compare;
        }

        public int Compare(SectionView a, SectionView b) => _compare(a, b);
    }
}
