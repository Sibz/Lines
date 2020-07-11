using Unity.Entities;

namespace Sibz.Lines
{
    public struct LineToolFinishEvent : IComponentData
    {
        public static void New()
        {
            LineDataWorld.World.EntityManager.CreateEntity(typeof(LineToolFinishEvent));
        }
    }
}