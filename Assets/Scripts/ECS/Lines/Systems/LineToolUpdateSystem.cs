using System;
using Sibz.Lines.ECS.Behaviours;
using Sibz.Lines.ECS.Components;
using Sibz.Lines.ECS.Enums;
using Sibz.Lines.ECS.Events;
using Sibz.Lines.ECS.Jobs;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Sibz.Lines.ECS.Systems
{
    public class LineToolUpdateSystem : SystemBase
    {
        private EntityQuery eventQuery;
        private EntityQuery updateEventQuery;
        private EntityQuery joinPointQuery;

        protected override void OnCreate()
        {
            RequireSingletonForUpdate<LineTool>();
            updateEventQuery = GetEntityQuery(typeof(NewLineUpdateEvent));
            // TODO: Update only editable join points
            joinPointQuery = GetEntityQuery(typeof(LineJoinPoint));
        }

        protected override void OnUpdate()
        {
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

            Dependency = Entities
                .WithStoreEntityQueryInField(ref eventQuery)
                .WithDeallocateOnJobCompletion(joinPointEntities)
                .WithDeallocateOnJobCompletion(joinPoints)
                .ForEach((Entity eventEntity, int entityInQueryIndex, ref NewLineUpdateEvent lineUpdateEvent) =>
                {
                    new LineToolUpdateJob
                    {
                        EventData = lineUpdateEvent,
                        Ecb = ecbConcurrent,
                        JobIndex = entityInQueryIndex,
                        JoinPointEntities = joinPointEntities,
                        JoinPoints = joinPoints
                    }.Execute(ref lineTool);
                }).Schedule(Dependency);

            ecb.DestroyEntity(eventQuery);
            LineEndSimBufferSystem.Instance.AddJobHandleForProducer(Dependency);
            SetSingleton(lineTool);
        }
    }
}