using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Nav3d.Octrees
{
    [StructLayout(LayoutKind.Explicit, Size = 72)]
    public unsafe struct OctreeNode
    {
        [FieldOffset(0)]
        internal byte ChildrensIsNodeFlags;

        [FieldOffset(1)]
        internal byte ChildrensIsValueFlags;

        [FieldOffset(8)]
        internal fixed long Children[8];
    }
}
