namespace DataStructure.Heap
{
    using System.Collections.Generic;
    using UnityEngine;

    public class Heap<T>
    {
        public class Node
        {
            public T data;
            public float priority;

            public Node(T data, float priority)
            {
                this.data = data;
                this.priority = priority;
            }

            public int CompareTo(Node other)
            {
                return priority.CompareTo(other.priority);
            }
        }

        protected List<Node> list = new ();
        public int Count => list.Count;

        public virtual void Add(T data, float priority)
        {
            Node node = new (data, priority);

            list.Add(node);

            int index = list.Count - 1;
            while (index > 0)
            {
                int parentIndex = (index - 1) / 2;

                if (list[index].CompareTo(list[parentIndex]) >= 0) {
                    break;
                }
                else {
                    (list[index], list[parentIndex]) = (list[parentIndex], list[index]);
                }

                index = parentIndex;
            }
        }

        public virtual T Remove()
        {
            if (list.Count == 0) { return default; }

            T result = list[0].data;

            list[0] = list[^1];
            list.RemoveAt(list.Count - 1);

            int lastIndex = list.Count - 1;
            int index = 0;
            while (index < list.Count - 1)
            {
                int smallestIndex = index;
                int leftChildIndex = smallestIndex * 2 + 1;
                int rightChildIndex = smallestIndex * 2 + 2;

                if (leftChildIndex <= lastIndex && list[smallestIndex].CompareTo(list[leftChildIndex]) >= 0)
                {
                    smallestIndex = leftChildIndex;
                }
                if (rightChildIndex <= lastIndex && list[smallestIndex].CompareTo(list[rightChildIndex]) >= 0)
                {
                    smallestIndex = rightChildIndex;
                }

                if (index == smallestIndex) { break; }

                (list[index], list[smallestIndex]) = (list[smallestIndex], list[index]);
                index = smallestIndex;
            }

            return result;
        }

        public void Print()
        {
            string result = string.Join(", ", list);
            Debug.Log(result);
        }
    }

    public class PriorityQueue<T>
    {
        protected Heap<T> heap = new ();
        public int Count => heap.Count;

        public void Enqueue(T data, float priority)
        {
            heap.Add(data, priority);
        }

        public T Dequeue()
        {
            return heap.Remove();
        }
    }
}