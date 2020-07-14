using Sibz.Lines.ECS.Behaviours;
using Sibz.Lines.ECS.Components;
using Unity.Entities;

namespace Sibz.Lines.ECS.Systems
{
    public class LineDirtySystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var commandBuffer = LineEndSimBufferSystem.Instance.CreateCommandBuffer();
            Entities
                .WithAll<Dirty>()
                .WithoutBurst()
                .ForEach((Entity entity, int entityInQueryIndex, ref Line line) =>
                {
                    LineWorld.Em.GetComponentObject<EcsLineBehaviour>(entity).OnDirty();
                    commandBuffer.RemoveComponent<Dirty>(entity);
                }).Run();
        }

    }
}