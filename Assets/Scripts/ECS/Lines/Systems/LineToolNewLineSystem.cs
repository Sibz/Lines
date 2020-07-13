using Sibz.Lines.ECS.Components;
using Sibz.Lines.ECS.Enums;
using Sibz.Lines.ECS.Events;
using Sibz.Lines.ECS.Jobs;
using Unity.Entities;

namespace Sibz.Lines.ECS.Systems
{
    [UpdateInGroup(typeof(LineWorldSimGroup), OrderFirst = true)]
    public class LineToolNewLineSystem : SystemBase
    {
        private EntityQuery query;
        protected override void OnCreate()
        {
            query = GetEntityQuery(typeof(NewLineEvent));
            RequireSingletonForUpdate<LineTool>();
            RequireForUpdate(query);
        }

        protected override void OnUpdate()
        {
            LineTool lineTool = GetSingleton<LineTool>();

            if (lineTool.State != LineToolState.Idle)
            {
                return;
            }

            Entities
                .WithStructuralChanges()
                .ForEach((Entity eventEntity, ref NewLineEvent newLineEvent) =>
                {
                    new LineToolCreateLineJob
                    {
                        EntityManager = EntityManager,
                        NewLineEvent = newLineEvent
                    }.Execute(ref lineTool);
                    EntityManager.DestroyEntity(eventEntity);
                }).WithoutBurst().Run();

            LineEndSimBufferSystem.Instance.AddJobHandleForProducer(Dependency);
            SetSingleton(lineTool);
        }
    }
}