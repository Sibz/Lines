using Unity.Entities;

namespace Sibz.Lines.ECS.Events
{
    public struct NewLineCompleteEvent : IComponentData
    {
        public static void New()
        {
            LineWorld.Em.CreateEntity(typeof(NewLineCompleteEvent));
        }
    }
}