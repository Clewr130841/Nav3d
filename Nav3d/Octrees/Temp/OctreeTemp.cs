using Nav3d.Octree;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nav3d.Octrees.Temp
{
    public class OctreeTemp : OctreeBase
    {
        public Queue<long> _returnedAddresses;

        public List<OctreeNode> _octree;

        public OctreeTemp(OctreeSettings settings) : base(settings)
        {
            _returnedAddresses = new Queue<long>();
            _octree = new List<OctreeNode>()
            {
                new OctreeNode()
            };
        }

        protected override void Flush()
        {
        }

        protected override void GetNodeByAddress(long address, out OctreeNode node)
        {
            var intAddress = (int)address;
            node = _octree[intAddress];
        }

        protected override long GetRootNodeAddress()
        {
            return 0;
        }

        protected override long RentNewNodeAddress()
        {
            if (_returnedAddresses.Count == 0)
            {
                _octree.Add(new OctreeNode());
                return _octree.Count - 1;
            }
            else
            {
                return _returnedAddresses.Dequeue();
            }
        }

        protected override void ReturnNodeAddress(long address)
        {
            _returnedAddresses.Enqueue(address);
        }

        protected override void SetValueByAddress(long address, int depth, ref OctreeNode value)
        {
            _octree[(int)address] = value;
        }
    }
}
