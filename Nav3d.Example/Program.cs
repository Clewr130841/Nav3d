using Nav3d.Octree;
using Nav3d.Octrees.Temp;
using System.Numerics;

namespace Nav3d.Example
{
    internal class Program
    {
        static void Main(string[] args)
        {
            OctreeBase octree = new OctreeTemp(new Octrees.OctreeSettings());

            var testVector = new Vector3(14, 0.25f, 50.4f);

            octree.SetValue(testVector, 1234);

            var resultValue = octree.GetValue(testVector);

            Console.ReadLine();
        }
    }
}