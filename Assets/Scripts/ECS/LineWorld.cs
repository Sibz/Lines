using System;
using System.Linq;
using System.Reflection;
using Unity.Entities;

namespace Sibz.Lines.ECS
{
    public class LineWorld : World
    {
        private static LineWorld world;
        public static LineWorld World => world ?? (world = new LineWorld());
        public static EntityManager Em => World.EntityManager;
        public static LineWorldSimGroup SimGroup => World.GetOrCreateSystem<LineWorldSimGroup>();
        public LineWorld() : base("Line Data World")
        {
        }

        public void Initialise()
        {
            foreach (Type type in Assembly.GetAssembly(typeof(LineWorld)).GetTypes().Where(x => x.IsSubclassOf(typeof(SystemBase))))
            {
                SimGroup.AddSystemToUpdateList(World.GetOrCreateSystem(type));
            }
        }
    }
}