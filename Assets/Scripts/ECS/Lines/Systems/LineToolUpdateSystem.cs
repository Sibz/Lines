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
        private EntityQuery updateEventQuery;
        private EntityQuery lineQuery;

        protected override void OnCreate()
        {
            updateEventQuery = GetEntityQuery(typeof(NewLineUpdateEvent));
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

            NativeArray<Entity> lineEntities = lineQuery.ToEntityArrayAsync(Allocator.TempJob, out JobHandle jh1);
            NativeArray<Line> lines = lineQuery.ToComponentDataArrayAsync<Line>(Allocator.TempJob, out JobHandle jh2);

            Dependency = JobHandle.CombineDependencies(Dependency, jh1, jh2);

            NativeArray<LineTool> toolData = new NativeArray<LineTool>(1, Allocator.TempJob);
            toolData[0] = lineTool;

            GatherJoinPoints(
                lineTool.Data.LineEntity,
                out NewLineUpdateEvent lineUpdateEvent, out NativeArray<Entity> joinPointEntities, out NativeArray<LineJoinPoint> joinPoints);

            Dependency = new LineToolUpdateJob
            {
                LineTool = toolData,
                EventData = lineUpdateEvent,
                Ecb = LineEndSimBufferSystem.Instance.CreateCommandBuffer(),
                JoinPointEntities = joinPointEntities,
                JoinPoints = joinPoints,
                Lines = lines,
                LineEntities = lineEntities,
                LineToolEntity = lineToolEntity
            }.Schedule(Dependency);

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
                Ecb = LineEndSimBufferSystem.Instance.CreateCommandBuffer(),
                LineEntity = lineTool.Data.LineEntity,
                //TODO: Load mesh builder from line profile
                MeshBuilderPrefab = LineDefaultMeshBuilderSystem.Prefab
            }.Schedule(Dependency);

            LineEndSimBufferSystem.Instance.AddJobHandleForProducer(Dependency);
        }

        private void GatherJoinPoints(Entity lineEntity, out NewLineUpdateEvent newLineUpdateEvent, out NativeArray<Entity> joinPointEntities, out NativeArray<LineJoinPoint> joinPoints)
        {
            NativeArray<Entity> evtEntityArray = updateEventQuery.ToEntityArray(Allocator.TempJob);
            NativeArray<NewLineUpdateEvent> evtArray = updateEventQuery.ToComponentDataArray<NewLineUpdateEvent>(Allocator.TempJob);
            Entity eventEntity = evtEntityArray[0];
            newLineUpdateEvent = evtArray[0];
            evtArray.Dispose();
            evtEntityArray.Dispose();
            Dependency.Complete();
            EntityManager.DestroyEntity(eventEntity);

            NativeList<Entity> joinPointEntitiesList = new NativeList<Entity>(2, Allocator.TempJob);
            NativeList<LineJoinPoint> joinPointsList = new NativeList<LineJoinPoint>(2, Allocator.TempJob);
            new LineToolUpdateGatherJoinsJob
            {
                Em = EntityManager,
                JoinEntities = joinPointEntitiesList,
                JoinPoints = joinPointsList,
                LineEntity = lineEntity,
                JoinToEntity = newLineUpdateEvent.JoinTo
            }.Schedule(Dependency).Complete();
            joinPointEntities = joinPointEntitiesList.ToArray(Allocator.TempJob);
            joinPointEntitiesList.Dispose();
            joinPoints = joinPointsList.ToArray(Allocator.TempJob);
            joinPointsList.Dispose();
        }
    }
}