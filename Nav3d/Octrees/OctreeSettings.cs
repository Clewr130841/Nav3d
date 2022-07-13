using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Nav3d.Octrees
{
    public class OctreeSettings
    {
        /// <summary>
        /// Размер октодерева
        /// </summary>
        public float SizeOfField { get; set; } = 32768f; // 34133.33312f - Размер карты Wow, но мы подгоним значение под удобное

        /// <summary>
        /// Шаг индексации
        /// </summary>
        public float Step { get; set; } = 0.5f;
    }
}
