using Nav3d.Octrees;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Nav3d.Octree
{
    /// <summary>
    /// Base class of octree field
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public unsafe abstract class OctreeBase
    {
        private OctreeSettings _settings;
        private float[] _depthOffsets;
        private int _maxDepth;

        public OctreeBase(OctreeSettings settings)
        {
            _settings = settings;
            Init();
        }


        #region Abstract interface
        protected abstract void GetNodeByAddress(long address, out OctreeNode node);

        protected abstract void SetValueByAddress(long address, int depth, ref OctreeNode value);

        protected abstract long GetRootNodeAddress();

        protected abstract long RentNewNodeAddress();

        protected abstract void ReturnNodeAddress(long address);

        protected abstract void Flush();
        #endregion


        private void Init()
        {
            //Считаем глубину дерева и отступы, необходимые на каждом уровне глубины

            var depthOffsets = new List<float>();
            var maxOffset = _settings.SizeOfField / 2f;

            for (var currentOffset = _settings.Step / 2f; currentOffset <= maxOffset; currentOffset *= 2f)
            {
                depthOffsets.Add(currentOffset);
            }

            depthOffsets.Reverse();

            _depthOffsets = depthOffsets.ToArray();

            _maxDepth = _depthOffsets.Length - 1;
        }

        private int GetNodeIndex(Vector3 coords, Vector3 center)
        {
            if (coords.X > center.X)
            {
                if (coords.Y > center.Y)
                {
                    if (coords.Z > center.Z)
                    {
                        return 0;
                    }
                    else
                    {
                        return 1;
                    }
                }
                else
                {
                    if (coords.Z > center.Z)
                    {
                        return 2;
                    }
                    else
                    {
                        return 3;
                    }
                }
            }
            else
            {
                if (coords.Y > center.Y)
                {
                    if (coords.Z > center.Z)
                    {
                        return 4;
                    }
                    else
                    {
                        return 5;
                    }
                }
                else
                {
                    if (coords.Z > center.Z)
                    {
                        return 6;
                    }
                    else
                    {
                        return 7;
                    }
                }
            }
        }

        private Vector3 GetChildNodeCenter(int index, int depth, Vector3 parentCenter)
        {
            var offset = _depthOffsets[depth];

            switch (index)
            {
                case 0:
                    return new Vector3(parentCenter.X + offset, parentCenter.Y + offset, parentCenter.Z + offset);
                case 1:
                    return new Vector3(parentCenter.X + offset, parentCenter.Y + offset, parentCenter.Z - offset);
                case 2:
                    return new Vector3(parentCenter.X + offset, parentCenter.Y - offset, parentCenter.Z + offset);
                case 3:
                    return new Vector3(parentCenter.X + offset, parentCenter.Y - offset, parentCenter.Z - offset);
                case 4:
                    return new Vector3(parentCenter.X - offset, parentCenter.Y + offset, parentCenter.Z + offset);
                case 5:
                    return new Vector3(parentCenter.X - offset, parentCenter.Y + offset, parentCenter.Z - offset);
                case 6:
                    return new Vector3(parentCenter.X - offset, parentCenter.Y - offset, parentCenter.Z + offset);
                case 7:
                    return new Vector3(parentCenter.X - offset, parentCenter.Y - offset, parentCenter.Z - offset);
                default:
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        private long? GetValue(long nodeAddress, Vector3 coords, Vector3 center, int depth)
        {
            GetNodeByAddress(nodeAddress, out OctreeNode node);

            var childIndex = GetNodeIndex(coords, center);

            if (node.ChildIsNode(childIndex))
            {
                var childCenter = GetChildNodeCenter(childIndex, depth, center);
                return GetValue(node.Children[childIndex], coords, childCenter, depth + 1);
            }
            else if (node.ChildIsValue(childIndex))
            {
                return node.Children[childIndex];
            }
            else
            {
                return null;
            }
        }

        private void SetValue(long nodeAddress, ref OctreeNode node, Vector3 coords, Vector3 center, int depth, long newValue, out bool changed, out bool canBeOptimized)
        {
            var childIndex = GetNodeIndex(coords, center);

            // Если глубина соответствует максимальной, просто ставим значение и флаги
            if (depth == _maxDepth)
            {
                canBeOptimized = changed = node.SetChildAsValue(childIndex, newValue);
                return;
            }

            bool childChanged, childCanBeOptimized;
            Vector3 childCenter;

            
            if (node.ChildIsNode(childIndex)) // Если по дочернему индексу лежит нода
            {
                // Получаем адрес дочерней ноды
                var childNodeAddress = node.Children[childIndex];
                // Получаем саму дочернюю ноду
                GetNodeByAddress(childNodeAddress, out OctreeNode childNode);
                // Считаем центр для дочерней ноды
                childCenter = GetChildNodeCenter(childIndex, depth, center);
                // Проваливаемся в процедуру и выясняем, будет ли обновлено значение и нужно ли пробовать оптимизировать ноду
                SetValue(childNodeAddress, ref childNode, coords, childCenter, depth + 1, newValue, out childChanged, out childCanBeOptimized);
                
                if(childCanBeOptimized) // Если нода ниже отметила, что можно пробовать оптимизировать
                {
                    //Проверяем, может ли дочернаяя нода быть оптимизирована
                    if(childNode.CanBeOptimized())
                    {
                        // Передаем выше, что нода поменялась и можно попробовать оптимизировать
                        canBeOptimized = changed = true;
                        // Оптимизируем ноду
                        node.SetChildAsValue(childIndex, newValue);
                        // Возвращаем адрес ноды в пул
                        ReturnNodeAddress(childNodeAddress);
                        // Выходим
                        return;
                    }
                }

                if(childChanged) // Если  нода изменилась ее надо записать
                {
                    SetValueByAddress(childNodeAddress, depth + 1, ref childNode);
                }

                // Передаем выше, там не пробовали оптимизировать
                canBeOptimized = changed = false;
            }
            else if (node.ChildIsValue(childIndex)) // Если по дочернему индексу лежит значение
            {
                if (node.ChildIsEquals(childIndex, newValue)) // Если оно такое же, ничего делать не нужно
                {
                    canBeOptimized = changed = false;
                    return;
                }
                else //Если значение другое, придется создать ноду
                {
                    // Получаем адрес для новой ноды
                    var newChildNodeAddress = RentNewNodeAddress();
                    
                    // Создаем новую ноду
                    var newChildNode = new OctreeNode();
                    
                    // Заполняем все значение новой ноды, значениями, которые были выше
                    newChildNode.FillChildrenWithValue(node.Children[childIndex]);
                    
                    // Считаем центр для новой ноды
                    childCenter = GetChildNodeCenter(childIndex, depth, center);
                    
                    // Устанавливаем значение для нее
                    SetValue(newChildNodeAddress, ref newChildNode, coords, childCenter, depth + 1, newValue, out _, out _);
                    
                    // Записываем новую ноду по адресу
                    SetValueByAddress(newChildNodeAddress, depth + 1, ref newChildNode);

                    // Записываем по индексу адрес новой ноды
                    node.SetChildAsNode(childIndex, newChildNodeAddress);

                    // Ставим флаги, что значения изменились и оптимизировать выше не нужно
                    changed = true;
                    canBeOptimized = false;
                }
            }
            else // Если в дочерней ноде совсем пусто
            {
                // Получаем новый адрес для ноды
                var newChildNodeAddress = RentNewNodeAddress();
                // Создаем новую ноду
                var newChildNode = new OctreeNode();
                
                // Считаем центр для новой ноды
                childCenter = GetChildNodeCenter(childIndex, depth, center);

                // Проваливаемся в процедуру рекурсивно, чтобы выставить значение, флаги для выяснения внутренних изменений нам не нужны,
                // т.к. заранее знаем, что нода поменялась и мы ее уже записали
                SetValue(newChildNodeAddress, ref newChildNode, coords, childCenter, depth + 1, newValue, out _, out _);

                // Записываем новую ноду по выданному адресу
                SetValueByAddress(newChildNodeAddress, depth + 1, ref newChildNode);

                // Говорим, что теперь, по дочернему адрему лежит нода
                node.SetChildAsNode(childIndex, newChildNodeAddress);

                // Ставим флаги изменения и возможности для оптимизации выше, оптимизировать не нужно, т.к. тут лежит нода, а не значение
                canBeOptimized = false;
                changed = true;
            }
        }

        public long? GetValue(Vector3 coords)
        {
            var rootNodeAddress = GetRootNodeAddress();
            return GetValue(rootNodeAddress, coords, Vector3.Zero, 0);
        }

        public void SetValue(Vector3 coords, long newValue)
        {
            var rootNodeAddress = GetRootNodeAddress();
            GetNodeByAddress(rootNodeAddress, out OctreeNode rootNode);

            SetValue(rootNodeAddress, ref rootNode, coords, Vector3.Zero, 0, newValue, out bool rootChanged, out _);

            if (rootChanged)
            {
                SetValueByAddress(rootNodeAddress, 0, ref rootNode);
            }
        }
    }
}
