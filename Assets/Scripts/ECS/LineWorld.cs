using Sibz.Lines.ECS.Systems;
using Unity.Entities;

namespace Sibz.Lines.ECS
{
    public class LineWorld : World
    {
        public static LineWorld World;
        public static EntityManager Em => World.EntityManager;

        public readonly SimulationSystemGroup SimGroup;
        public LineWorld() : base("Line Data World")
        {
            SimGroup = CreateSystem<SimulationSystemGroup>();
            SimGroup.AddSystemToUpdateList(CreateSystem<NewLineSystem>());
        }
    }
}