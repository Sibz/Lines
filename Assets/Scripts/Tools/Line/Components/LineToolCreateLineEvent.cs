using Unity.Entities;
using Unity.Mathematics;

namespace Sibz.Lines
{
    public struct LineToolCreateLineEvent : IComponentData
    {
        public float3 Position;

        public static void New(float3 position)
        {
            var entity = LineDataWorld.World.EntityManager.CreateEntity(typeof(LineToolCreateLineEvent));
            LineDataWorld.World.EntityManager.SetComponentData(entity, new LineToolCreateLineEvent { Position =  position});
        }
    }
}