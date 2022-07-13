using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Nav3d.Octrees
{
    internal unsafe static class OctreeNodeOperationsExtensions
    {
        static readonly byte[] NODE_SET_FLAGS = new byte[] { 1, 2, 4, 8, 16, 32, 64, 128 };

        static readonly byte[] NODE_RESET_FLAGS = new byte[] {
            (byte)~NODE_SET_FLAGS[0],
            (byte)~NODE_SET_FLAGS[1],
            (byte)~NODE_SET_FLAGS[2],
            (byte)~NODE_SET_FLAGS[3],
            (byte)~NODE_SET_FLAGS[4],
            (byte)~NODE_SET_FLAGS[5],
            (byte)~NODE_SET_FLAGS[6],
            (byte)~NODE_SET_FLAGS[7],
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ChildIsNode(this ref OctreeNode node, int childIndex)
        {
            return (node.ChildrensIsNodeFlags & NODE_SET_FLAGS[childIndex]) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ChildIsValue(this ref OctreeNode node, int childIndex)
        {
            return (node.ChildrensIsValueFlags & NODE_SET_FLAGS[childIndex]) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ChildIsEquals(this ref OctreeNode node, int childIndex, long value)
        {
            return node.Children[childIndex] == value;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FillChildrenWithValue(this ref OctreeNode node, long value)
        {
            node.ChildrensIsNodeFlags = byte.MinValue;
            node.ChildrensIsValueFlags = byte.MaxValue;
            for (var i = 0; i < 8; i++)
            {
                node.Children[i] = value;
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SetChildAsValue(this ref OctreeNode node, int childIndex, long value)
        {
            if (node.ChildIsValue(childIndex) && node.ChildIsEquals(childIndex, value))
            {
                return false;
            }

            node.ResetChildIsNode(childIndex);
            node.SetChildIsValue(childIndex);
            node.Children[childIndex] = value;

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CanBeOptimized(this ref OctreeNode node)
        {
            // Если флаги нам говорят, что все лежащие дочерние итемы это значения
            if(node.ChildrensIsValueFlags == byte.MaxValue)
            {
                // Проверяем все ли они одинаковые
                for(var i = 1; i < 8; i++)
                {
                    if(node.Children[i] != node.Children[0])
                    {
                        return false;
                    }
                }
                return true; // Нода может быть оптимизирована
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetChildAsNode(this ref OctreeNode node, int childIndex, long nodeAddress)
        {
            node.ResetChildIsValue(childIndex);
            node.SetChildIsNode(childIndex);
            node.Children[childIndex] = nodeAddress;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ClearChild(this ref OctreeNode node, int childIndex)
        {
            node.ResetChildIsNode(childIndex);
            node.ResetChildIsValue(childIndex);
            node.Children[childIndex] = 0;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void SetChildIsNode(this ref OctreeNode node, int childIndex)
        {
            node.ChildrensIsNodeFlags = (byte)(node.ChildrensIsNodeFlags | NODE_SET_FLAGS[childIndex]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void ResetChildIsNode(this ref OctreeNode node, int childIndex)
        {
            node.ChildrensIsNodeFlags = (byte)(node.ChildrensIsNodeFlags & NODE_RESET_FLAGS[childIndex]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void SetChildIsValue(this ref OctreeNode node, int childIndex)
        {
            node.ChildrensIsValueFlags = (byte)(node.ChildrensIsValueFlags | NODE_SET_FLAGS[childIndex]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void ResetChildIsValue(this ref OctreeNode node, int childIndex)
        {
            node.ChildrensIsValueFlags = (byte)(node.ChildrensIsValueFlags & NODE_RESET_FLAGS[childIndex]);
        }


    }
}
