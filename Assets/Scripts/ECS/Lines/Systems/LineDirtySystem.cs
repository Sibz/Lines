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
                    new LineDirtyJob
                    {
                        LineEntity =  entity,

                    }.Execute();

                    commandBuffer.RemoveComponent<Dirty>(entity);
                }).Run();
        }

        public struct LineDirtyJob
        {
            public Entity LineEntity;
            private EcsLineBehaviour lineBehaviour;

            public void Execute()
            {
                lineBehaviour = LineWorld.Em.GetComponentObject<EcsLineBehaviour>(LineEntity);
                lineBehaviour.EndNode1.UpdateFromEntity();
                lineBehaviour.EndNode2.UpdateFromEntity();
            }
        }
    }
}