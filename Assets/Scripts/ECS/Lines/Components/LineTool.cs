using Sibz.Lines.ECS.Behaviours;
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

        public EcsLineBehaviour LineBehaviour =>
            State == LineToolState.Editing && LineWorld.Em.Exists(Data.LineEntity)
                ? LineWorld.Em.GetComponentObject<EcsLineBehaviour>(Data.LineEntity)
                : null;
        public static Entity New()
        {
            return LineWorld.Em.CreateEntity(Archetype);
        }
    }
}