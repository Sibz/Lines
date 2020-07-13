using Sibz.Lines.ECS.Components;
using Sibz.Lines.ECS.Enums;
using Sibz.Lines.ECS.Events;
using Sibz.Lines.ECS.Jobs;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

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
            lineQuery = GetEntityQuery(typeof(Line), typeof(NewLine));

            RequireSingletonForUpdate<LineTool>();
            RequireForUpdate(updateEventQuery);

        }

        protected override void OnUpdate()
        {
            Entity lineToolEntity = GetSingletonEntity<LineTool>();

            LineTool lineTool = GetSingleton<LineTool>();

            if (lineTool.State != LineToolState.Editing)
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

            NativeArray<LineTool> toolData = new NativeArray<LineTool>(1, Allocator.TempJob);
            toolData[0] = lineTool;

            Dependency = Entities
                .WithStoreEntityQueryInField(ref eventQuery)
                .WithDeallocateOnJobCompletion(joinPointEntities)
                .WithDeallocateOnJobCompletion(joinPoints)
                .WithDeallocateOnJobCompletion(lineEntities)
                .WithDeallocateOnJobCompletion(lines)
                .ForEach((Entity eventEntity, int entityInQueryIndex, ref NewLineUpdateEvent lineUpdateEvent) =>
                {
                    var lineTool = toolData[0];
                    new LineToolUpdateJob
                    {
                        EventData = lineUpdateEvent,
                        Ecb = ecbConcurrent,
                        JobIndex = entityInQueryIndex,
                        JoinPointEntities = joinPointEntities,
                        JoinPoints = joinPoints,
                        Lines = lines,
                        LineEntities = lineEntities
                    }.Execute(ref lineTool);
                    toolData[0] = lineTool;
                    ecbConcurrent.SetComponent(entityInQueryIndex, lineToolEntity, toolData[0]);
                }).Schedule(Dependency);

            NativeArray<bool> didChange = new NativeArray<bool>(1, Allocator.TempJob);

            Dependency = new LineToolUpdateKnotsJob
            {
                ToolData = toolData,
                KnotData = EntityManager.GetBuffer<LineKnotData>(lineTool.Data.LineEntity),
                //TODO: Load line profile
                LineProfile = LineProfile.Default(),
                DidChange = didChange
            }.Schedule(Dependency);

            Dependency = new LineToolTriggerMeshRebuildJob
            {
                DidChange = didChange,
                JobIndex = eventQuery.CalculateEntityCount() + 1,
                Ecb = LineEndSimBufferSystem.Instance.CreateCommandBuffer().ToConcurrent(),
                LineEntity = lineTool.Data.LineEntity,
                //TODO: Load mesh builder from line profile
                MeshBuilderPrefab = LineDefaultMeshBuilderSystem.Prefab
            }.Schedule(Dependency);

            ecb.DestroyEntity(eventQuery);
            LineEndSimBufferSystem.Instance.AddJobHandleForProducer(Dependency);
        }
    }
}