using Sibz.Lines.ECS.Components;
using Sibz.Lines.ECS.Enums;
using Sibz.Lines.ECS.Events;
using Sibz.Lines.ECS.Jobs;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Sibz.Lines.ECS.Systems
{
    public class LineToolUpdateSystem : SystemBase
    {
        private EntityQuery eventQuery;
        private EntityQuery updateEventQuery;
        private EntityQuery joinPointQuery;
        private EntityQuery lineQuery;

        protected override void OnCreate()
        {

            updateEventQuery = GetEntityQuery(typeof(NewLineUpdateEvent));
            // TODO: Update only editable join points
            joinPointQuery = GetEntityQuery(typeof(LineJoinPoint));
            // TODO: Remove NewLine on completion
            lineQuery = GetEntityQuery(typeof(Line), typeof(NewLine));

            RequireSingletonForUpdate<LineTool>();
            RequireForUpdate(eventQuery);
        }

        protected override void OnUpdate()
        {
            Entity lineToolEntity = GetSingletonEntity<LineTool>();
            LineTool lineTool = GetSingleton<LineTool>();

            if (lineTool.State != LineToolState.Editing || updateEventQuery.CalculateEntityCount() == 0)
            {
                return;
            }

            var ecb = LineEndSimBufferSystem.Instance.CreateCommandBuffer();
            var ecbConcurrent = ecb.ToConcurrent();
            var joinPointEntities = joinPointQuery.ToEntityArrayAsync(Allocator.TempJob, out JobHandle jh1);
            var joinPoints =
                joinPointQuery.ToComponentDataArrayAsync<LineJoinPoint>(Allocator.TempJob, out JobHandle jh2);

            Dependency = JobHandle.CombineDependencies(Dependency, jh1, jh2);

            var lineEntities = lineQuery.ToEntityArrayAsync(Allocator.TempJob, out jh1);
            var lines = lineQuery.ToComponentDataArrayAsync<Line>(Allocator.TempJob, out jh2);

            Dependency = JobHandle.CombineDependencies(Dependency, jh1, jh2);


            Dependency = Entities
                .WithStoreEntityQueryInField(ref eventQuery)
                .WithDeallocateOnJobCompletion(joinPointEntities)
                .WithDeallocateOnJobCompletion(joinPoints)
                .WithDeallocateOnJobCompletion(lineEntities)
                .WithDeallocateOnJobCompletion(lines)
                .ForEach((Entity eventEntity, int entityInQueryIndex, ref NewLineUpdateEvent lineUpdateEvent) =>
                {
                    new LineToolUpdateJob
                    {
                        EventData = lineUpdateEvent,
                        Ecb = ecbConcurrent,
                        JobIndex = entityInQueryIndex,
                        JoinPointEntities = joinPointEntities,
                        JoinPoints = joinPoints,
                        Lines  = lines,
                        LineEntities = lineEntities
                    }.Execute(ref lineTool);
                    ecbConcurrent.SetComponent(entityInQueryIndex, lineToolEntity, lineTool);
                }).Schedule(Dependency);

            Dependency = new LineToolUpdateKnotsJob
            {
                LineTool = lineTool,
                KnotData = EntityManager.GetBuffer<LineKnotData>(lineTool.Data.LineEntity),
                //TODO: Load line profile
                LineProfile = LineProfile.Default()
            }.Schedule(Dependency);

            ecb.DestroyEntity(eventQuery);
            LineEndSimBufferSystem.Instance.AddJobHandleForProducer(Dependency);
        }
    }
}