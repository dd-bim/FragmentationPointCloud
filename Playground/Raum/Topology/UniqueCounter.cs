using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Playground.Raum.Topology
{
    internal class UniqueCounter
    {
        private long _nextId = 1;
        private readonly Queue<long> _freeIds = new();
        private readonly object _lock = new();

        public long GetNextId()
        {
            lock (_lock)
            {
                return _freeIds.Count > 0 ? _freeIds.Dequeue() : _nextId++;
            }
        }

        public void ReleaseId(long id)
        {
            lock (_lock)
            {
                _freeIds.Enqueue(id);
            }
        }
    }
}
