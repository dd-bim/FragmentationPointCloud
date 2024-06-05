using System;
using System.Collections.Generic;
using System.Text;

namespace ScantraIO
{
    internal class Node<T>
    {
        public Node<T>? Next { get; set; }

        public T Value { get; set; }

        public Node(T value)
        {
            Value = value;
        }

        public T[] List
        {
            get
            {
                Node<T> cur = this;
                var lst = new List<T>();
                lst.Add(cur.Value);
                while(cur.Next != null)
                {
                    cur = cur.Next;
                    lst.Add(cur.Value);
                }
                return lst.ToArray();
            }
        }
    }
}
