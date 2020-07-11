using Unity.Entities;
using Unity.Mathematics;

namespace Sibz.Lines
{
    public struct LineToolCreateLineEvent : IComponentData
    {
        public float3 Position;
        public float3 Direction;
        public LineJoinData JoinTo;

        public static void New(float3 position)
        {
            var entity = LineDataWorld.World.EntityManager.CreateEntity(typeof(LineToolCreateLineEvent));
            LineDataWorld.World.EntityManager.SetComponentData(entity, new LineToolCreateLineEvent { Position =  position});
        }

        public static void New(float3 position, LineJoinData joinTo, float3 direction = default)
        {
            var entity = LineDataWorld.World.EntityManager.CreateEntity(typeof(LineToolCreateLineEvent));
            LineDataWorld.World.EntityManager.SetComponentData(entity, new LineToolCreateLineEvent
            {
                Position =  position,
                JoinTo =   joinTo,
                Direction = direction
            });
        }
    }
}