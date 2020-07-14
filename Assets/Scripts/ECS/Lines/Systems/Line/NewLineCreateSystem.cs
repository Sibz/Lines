using Sibz.Lines.ECS.Components;
using Sibz.Lines.ECS.Enums;
using Sibz.Lines.ECS.Events;
using Sibz.Lines.ECS.Jobs;
using Unity.Entities;

namespace Sibz.Lines.ECS.Systems
{
    [UpdateInGroup(typeof(LineWorldSimGroup), OrderFirst = true)]
    public class NewLineCreateSystem : SystemBase
    {
        private EntityQuery query;

        protected override void OnCreate()
        {
            query = GetEntityQuery(typeof(NewLineCreateEvent));
            RequireSingletonForUpdate<LineTool>();
            RequireForUpdate(query);
        }

        protected override void OnUpdate()
        {
            var lineTool = GetSingleton<LineTool>();

            if (lineTool.State != LineToolState.Idle) return;

            Entities
               .WithStructuralChanges()
               .ForEach((Entity eventEntity, ref NewLineCreateEvent newLineEvent) =>
                        {
                            new LineToolCreateLineJob
                            {
                                EntityManager      = EntityManager,
                                NewLineCreateEvent = newLineEvent
                            }.Execute(ref lineTool);
                            EntityManager.DestroyEntity(eventEntity);
                        }).WithoutBurst().Run();

            LineEndSimBufferSystem.Instance.AddJobHandleForProducer(Dependency);
            SetSingleton(lineTool);
        }
    }
}