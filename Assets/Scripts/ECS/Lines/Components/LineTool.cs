using Sibz.Lines.ECS.Enums;
using Unity.Entities;

namespace Sibz.Lines.ECS.Components
{
    public struct LineTool : IComponentData
    {
        public static EntityArchetype Archetype = LineWorld.Em
            .CreateArchetype(typeof(LineTool));
        public LineToolState State;
        public LineToolData Data;

        public static Entity New()
        {
            return LineWorld.Em.CreateEntity(Archetype);
        }
    }
}