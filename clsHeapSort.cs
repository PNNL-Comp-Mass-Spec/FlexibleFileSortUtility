using System.Collections.Generic;

// ReSharper disable UnusedMember.Global

namespace FlexibleFileSortUtility
{
    /// <summary>
    /// Heap sort implementation
    /// </summary>
    /// <remarks>From http://www.sinbadsoft.com/blog/binary-heap-heap-sort-and-priority-queue/ </remarks>
    /// <typeparam name="T"></typeparam>
    public static class HeapSort<T>
    {
        public static void Sort(IList<T> list, IComparer<T> comparer)
        {
            var heap = new Heap<T>(list, list.Count, comparer);
            while (heap.Count > 0)
            {
                heap.PopRoot();
            }
        }
    }
}
