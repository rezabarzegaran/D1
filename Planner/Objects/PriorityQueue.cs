using System;
using System.Collections.Generic;

namespace Planner.Objects
{
    // Thanks to James McCaffrey for making such a performant priority queue, when MS couldn't bother with it.
    // https://visualstudiomagazine.com/Articles/2012/11/01/Priority-Queues-with-C.aspx?Page=1
    // James McCaffrey 11/02/2012
    public class PriorityQueue<T>
    {
        private List<T> _tasks;
        private Func<T, T, int> _compare;
        private int _count;

        public PriorityQueue(Func<T, T, int> comparer)
        {
            this._tasks = new List<T>();
            _compare = comparer;
            _count = 0;
        }

        public void Enqueue(T item)
        {
            _tasks.Add(item);
            _count++;
            int ci = _tasks.Count - 1; // child index; start at end
            while (ci > 0)
            {
                int pi = (ci - 1) / 2; // parent index
                if (_compare(_tasks[ci], _tasks[pi]) >= 0) break; // child item is larger than (or equal) parent so we're done
                T tmp = _tasks[ci]; _tasks[ci] = _tasks[pi]; _tasks[pi] = tmp;
                ci = pi;
            }
        }
        public T Dequeue()
        {
            // assumes pq is not empty; up to calling code
            int li = _count - 1; // last index (before removal)
            T frontItem = _tasks[0];   // fetch the front
            _tasks[0] = _tasks[li];
            _tasks.RemoveAt(li);
            _count--;
            --li; // last index (after removal)
            int pi = 0; // parent index. start at front of pq
            while (true)
            {
                int ci = pi * 2 + 1; // left child index of parent
                if (ci > li) break;  // no children so done
                int rc = ci + 1;     // right child
                if (rc <= li && _compare(_tasks[rc], _tasks[ci]) < 0) // if there is a rc (ci + 1), and it is smaller than left child, use the rc instead
                    ci = rc;
                if (_compare(_tasks[pi], _tasks[ci]) <= 0) break; // parent is smaller than (or equal to) smallest child so done
                T tmp = _tasks[pi]; _tasks[pi] = _tasks[ci]; _tasks[ci] = tmp; // swap parent and child
                pi = ci;
            }
            return frontItem;
        }
        public T Peek()
        {
            if (_count == 0) return default(T);
            T frontItem = _tasks[0];
            return frontItem;
        }
        public int Count()
        {
            return _count;
        }
        public override string ToString()
        {
            string s = "";
            for (int i = 0; i < _tasks.Count; ++i)
                s += _tasks[i].ToString() + " ";
            s += "count = " + _tasks.Count;
            return s;
        }
        public bool IsConsistent()
        {
            // is the heap property true for all data?
            if (_count == 0) return true;
            int li = _count - 1; // last index
            for (int pi = 0; pi < _tasks.Count; ++pi) // each parent index
            {
                int lci = 2 * pi + 1; // left child index
                int rci = 2 * pi + 2; // right child index

                if (lci <= li && _compare(_tasks[pi], _tasks[lci]) > 0) return false; // if lc exists and it's greater than parent then bad.
                if (rci <= li && _compare(_tasks[pi], _tasks[rci]) > 0) return false; // check the right child too.
            }
            return true; // passed all checks
        }
    }
}
