using Sibz.Lines.ECS.Components;
using Sibz.Lines.ECS.Events;
using Sibz.Lines.ECS.Jobs;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Sibz.Lines.ECS.Systems
{
    public class NewLineUpdateSystem : SystemBase
    {
        private EntityQuery eventQuery;

        protected override void OnCreate()
        {
            eventQuery = GetEntityQuery(typeof(NewLineUpdateEvent));
            RequireForUpdate(eventQuery);
        }

        protected override void OnUpdate()
        {
            var eventCount = eventQuery.CalculateEntityCount();

            var lineWithJoinData =
                new NativeArray<LineWithJoinPointData>(eventCount, Allocator.TempJob);

            var eventData =
                eventQuery.ToComponentDataArrayAsync<NewLineUpdateEvent>(
                                                                         Allocator.TempJob, out var jh1);

            var joinPoints = GetComponentDataFromEntity<LineJoinPoint>();


            var lineEntities = new NativeArray<Entity>(eventCount, Allocator.TempJob);

            Dependency = new GatherLineWithJoinPointData
                         {
                             EventData        = eventData,
                             Lines            = GetComponentDataFromEntity<Line>(true),
                             JoinPoints       = joinPoints,
                             LineWithJoinData = lineWithJoinData,
                             LineEntities     = lineEntities
                         }.Schedule(eventCount, 4, JobHandle.CombineDependencies(Dependency, jh1));

            // This job only runs if UpdateJoinPoints is set in event data
            Dependency = new NewLineUpdateJoinPointJob
                         {
                             Ecb              = LineEndSimBufferSystem.Instance.CreateCommandBuffer().ToConcurrent(),
                             EventData        = eventData,
                             LineWithJoinData = lineWithJoinData,
                             JoinPoints       = joinPoints
                         }.Schedule(eventCount, 4, Dependency);

            var bezierData = new NativeArray<BezierData>(eventCount, Allocator.TempJob);

            Dependency = new NewLineGetBezierJob
                         {
                             Ecb              = LineEndSimBufferSystem.Instance.CreateCommandBuffer().ToConcurrent(),
                             BezierData       = bezierData,
                             JoinPoints       = GetComponentDataFromEntity<LineJoinPoint>(),
                             LineTool         = GetSingleton<LineTool>(),
                             LineWithJoinData = lineWithJoinData
                         }.Schedule(eventCount, 4, Dependency);

            Dependency = new NewLineGenerateKnotsJob
                         {
                             BezierData       = bezierData,
                             KnotData         = GetBufferFromEntity<LineKnotData>(),
                             LineEntities     = lineEntities,
                             LineProfiles     = GetComponentDataFromEntity<LineProfile>(),
                             LineTool         = GetSingleton<LineTool>(),
                             LineWithJoinData = lineWithJoinData
                         }.Schedule(eventCount, 4, Dependency);

            Dependency = new LineSetDirtyJob
                         {
                             LineEntities = lineEntities,
                             Ecb          = LineEndSimBufferSystem.Instance.CreateCommandBuffer().ToConcurrent()
                         }.Schedule(eventCount, 4, Dependency);

            Dependency = new LineTriggerMeshRebuildJob
                         {
                             Ecb              = LineEndSimBufferSystem.Instance.CreateCommandBuffer().ToConcurrent(),
                             LineEntities     = lineEntities,
                             LineProfiles     = GetComponentDataFromEntity<LineProfile>(),
                             LineWithJoinData = lineWithJoinData,
                             DefaultPrefab    = LineDefaultMeshBuilderSystem.Prefab
                         }.Schedule(eventCount, 4, Dependency);

            Dependency = new DeallocateJob<Entity, LineWithJoinPointData, NewLineUpdateEvent>
                         {
                             NativeArray1 = lineEntities,
                             NativeArray2 = lineWithJoinData,
                             NativeArray3 = eventData
                         }.Schedule(Dependency);

            LineEndSimBufferSystem.Instance.CreateCommandBuffer().DestroyEntity(eventQuery);

            LineEndSimBufferSystem.Instance.AddJobHandleForProducer(Dependency);
        }
    }
}