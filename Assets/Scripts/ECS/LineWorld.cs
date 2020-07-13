using System;
using System.Linq;
using System.Reflection;
using Sibz.Lines.ECS.Systems;
using Unity.Entities;

namespace Sibz.Lines.ECS
{
    public class LineWorld : World
    {
        private static LineWorld world;
        public static LineWorld World => world ?? (world = new LineWorld());
        public static EntityManager Em => World.EntityManager;
        public static LineWorldSimGroup SimGroup => World.GetOrCreateSystem<LineWorldSimGroup>();
        public static LineWorldPresGroup PresGroup => World.GetOrCreateSystem<LineWorldPresGroup>();

        public static LineWorldInitGroup InitGroup => World.GetOrCreateSystem<LineWorldInitGroup>();

        public LineWorld() : base("Line Data World")
        {
        }

        public void Initialise()
        {
            foreach (Type type in Assembly.GetAssembly(typeof(LineWorld)).GetTypes()
                .Where(x =>
                    (x.IsSubclassOf(typeof(SystemBase)) || x.IsSubclassOf(typeof(ComponentSystemBase)))
                    && x != typeof(LineWorldSimGroup)
                    && x != typeof(LineWorldPresGroup)
                    && x != typeof(LineWorldInitGroup)))
            {
                var attr =
                Attribute.GetCustomAttribute(type, typeof(UpdateInGroupAttribute)) as UpdateInGroupAttribute;
                if (attr != null && attr.GroupType == typeof(LineWorldPresGroup))
                {
                    PresGroup.AddSystemToUpdateList(World.GetOrCreateSystem(type));
                }else if (attr != null && attr.GroupType == typeof(LineWorldInitGroup))
                {
                    InitGroup.AddSystemToUpdateList(World.GetOrCreateSystem(type));
                }
                else
                {
                    SimGroup.AddSystemToUpdateList(World.GetOrCreateSystem(type));
                }
            }
        }
    }
}