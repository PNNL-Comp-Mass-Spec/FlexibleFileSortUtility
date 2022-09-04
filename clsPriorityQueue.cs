﻿using System.Collections.Generic;

namespace FlexibleFileSortUtility
{
    /// <summary>
    /// Priority queue implementation
    /// </summary>
    /// <remarks>From http://www.sinbadsoft.com/blog/binary-heap-heap-sort-and-priority-queue/ </remarks>
    /// <typeparam name="T"></typeparam>
    internal class PriorityQueue<T>
    {
        private readonly Heap<T> _heap;

        public PriorityQueue(IComparer<T> comparer)
        {
            _heap = new Heap<T>(new List<T>(), 0, comparer);
        }

        public int Size => _heap.Count;

        // ReSharper disable once UnusedMember.Global
        public T Top() { return _heap.PeekRoot(); }

        public void Push(T e) { _heap.Insert(e); }

        public T Pop() { return _heap.PopRoot(); }
    }
}
