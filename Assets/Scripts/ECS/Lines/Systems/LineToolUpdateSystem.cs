﻿using Sibz.Lines.ECS.Components;
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

            // This is no longer really a line tool system, it merely uses some state data from
            // line tool entity. This should probably be packaged into the event or simply
            // passed to the relevant functions
            if (lineTool.State != LineToolState.Editing)
            {
                return;
            }

            // So initially we need to use the entity manager to gather both lines and joins
            // for each event. We can then use IJobParallelFor over the resulting array

            // This query gets all new lines, however it's possible we could have events
            // to update lines that are not new, we need to gather all lines in the event queue
            NativeArray<Entity> lineEntities = lineQuery.ToEntityArrayAsync(Allocator.TempJob, out JobHandle jh1);
            NativeArray<Line> lines = lineQuery.ToComponentDataArrayAsync<Line>(Allocator.TempJob, out JobHandle jh2);

            Dependency = JobHandle.CombineDependencies(Dependency, jh1, jh2);

            NativeArray<LineTool> toolData = new NativeArray<LineTool>(1, Allocator.TempJob);
            toolData[0] = lineTool;

            // This needs to collect join points for any lines we are updating
            // while we use tool.data.lineEntity here, it should use line entity from
            // the event. Also might be better to put data into a single array of struct
            GatherJoinPoints(
                lineTool.Data.LineEntity,
                out NewLineUpdateEvent lineUpdateEvent, out NativeArray<Entity> joinPointEntities, out NativeArray<LineJoinPoint> joinPoints);

            // Our main job, updates our tool with bezier data
            // this is solely used for debug purposes so could
            // perhaps create/amend a debug data entity instead
            // This also does two things, updates the join points
            // so joins/un-joins the join point and updates the
            // pivot/direction
            // This should be split into two jobs, the update join
            // point job only needs to run if the update event had data
            // the bezier generation only needs to run if tool data
            // changed or if the event had data
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

            // Needs to update knots for each line that was modified
            // While we can use a chained job, we could ad the bezier needed as
            // a component and pick it up in another system
            Dependency = new LineToolUpdateKnotsJob
            {
                ToolData = toolData,
                KnotData = EntityManager.GetBuffer<LineKnotData>(lineTool.Data.LineEntity),
                //TODO: Load line profile
                LineProfile = LineProfile.Default(),
                DidChange = didChange
            }.Schedule(Dependency);

            // This spawns a new entity that triggers mesh building
            // As the mesh builder for each line profile could be different
            // this is spawned from a prefab that will trigger the relevant
            // mesh builder system to run
            // This may need to be triggered by other systems in future
            // so this should perhaps be an event trigger
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