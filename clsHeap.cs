﻿using System;
using System.Collections.Generic;

namespace FlexibleFileSortUtility
{
    /// <summary>
    /// Binary heap implementation
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <remarks>From http://www.sinbadsoft.com/blog/binary-heap-heap-sort-and-priority-queue/ </remarks>
    public class Heap<T>
    {
        private readonly IList<T> _list;
        private readonly IComparer<T> _comparer;

        public Heap(IList<T> list, int count, IComparer<T> comparer)
        {
            _comparer = comparer;
            _list = list;
            Count = count;
            Heapify();
        }

        public int Count { get; private set; }

        public T PopRoot()
        {
            if (Count == 0) throw new InvalidOperationException("Empty heap.");
            var root = _list[0];
            SwapCells(0, Count - 1);
            Count--;
            HeapDown(0);
            return root;
        }

        public T PeekRoot()
        {
            if (Count == 0) throw new InvalidOperationException("Empty heap.");
            return _list[0];
        }

        public void Insert(T e)
        {
            if (Count >= _list.Count) _list.Add(e);
            else _list[Count] = e;
            Count++;
            HeapUp(Count - 1);
        }

        private void Heapify()
        {
            for (var i = Parent(Count - 1); i >= 0; i--)
            {
                HeapDown(i);
            }
        }

        private void HeapUp(int i)
        {
            var elt = _list[i];
            while (true)
            {
                var parent = Parent(i);
                if (parent < 0 || _comparer.Compare(_list[parent], elt) > 0) break;
                SwapCells(i, parent);
                i = parent;
            }
        }

        private void HeapDown(int i)
        {
            while (true)
            {
                var lChild = LeftChild(i);
                if (lChild < 0) break;
                var rChild = RightChild(i);

                var child = rChild < 0
                  ? lChild
                  : _comparer.Compare(_list[lChild], _list[rChild]) > 0 ? lChild : rChild;

                if (_comparer.Compare(_list[child], _list[i]) < 0) break;
                SwapCells(i, child);
                i = child;
            }
        }

        private int Parent(int i) { return i <= 0 ? -1 : SafeIndex((i - 1) / 2); }

        private int RightChild(int i) { return SafeIndex(2 * i + 2); }

        private int LeftChild(int i) { return SafeIndex(2 * i + 1); }

        private int SafeIndex(int i) { return i < Count ? i : -1; }

        private void SwapCells(int i, int j)
        {
            (_list[i], _list[j]) = (_list[j], _list[i]);
        }
    }
}
