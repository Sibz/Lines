using Sibz.Lines.ECS.Components;
using Sibz.Lines.ECS.Events;
using Sibz.Lines.ECS.Jobs;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

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

            var lineJoinPoints =
                new NativeArray<JoinPointPair>(eventCount, Allocator.TempJob);

            var eventData =
                eventQuery.ToComponentDataArrayAsync<NewLineUpdateEvent>(
                                                                         Allocator.TempJob, out var jh1);

            var joinPoints = GetComponentDataFromEntity<LineJoinPoint>();


            var lineEntities = new NativeArray<Entity>(eventCount, Allocator.TempJob);

            Dependency = new LineGetJoinPointPairsJob
                         {
                             EventData      = eventData,
                             JoinPoints     = joinPoints,
                             LineJoinPoints = lineJoinPoints,
                             LineEntities   = lineEntities,
                             Lines          = GetComponentDataFromEntity<Line>()
                         }.Schedule(eventCount, 4, JobHandle.CombineDependencies(Dependency, jh1));

            // This job only runs if UpdateJoinPoints is set in event data
            Dependency = new NewLineGetUpdatedJoinPoints
                         {
                             Ecb            = LineEndSimBufferSystem.Instance.CreateCommandBuffer().ToConcurrent(),
                             EventData      = eventData,
                             LineJoinPoints = lineJoinPoints,
                             JoinPoints     = joinPoints,
                             Lines          = GetComponentDataFromEntity<Line>()
                         }.Schedule(eventCount, 4, Dependency);

            var updatedNewLines = new NativeArray<NewLine>(eventCount, Allocator.TempJob);

            Dependency = new NewLineUpdateModifiersJob
                         {
                             Ecb             = LineEndSimBufferSystem.Instance.CreateCommandBuffer().ToConcurrent(),
                             NewLines        = GetComponentDataFromEntity<NewLine>(),
                             LineEntities    = lineEntities,
                             LineJoinPoints  = lineJoinPoints,
                             UpdateEvents    = eventData,
                             UpdatedNewLines = updatedNewLines
                         }.Schedule(eventCount, 4, Dependency);

            var heightBeziers = new NativeArray<float2x4>(eventCount, Allocator.TempJob);

            Dependency = new NewLineCreateHeightBezierJob
                         {
                             UpdatedNewLines = updatedNewLines,
                             LineJoinPoints  = lineJoinPoints,
                             HeightBeziers   = heightBeziers
                         }.Schedule(eventCount, 4, Dependency);

            var bezierData = new NativeArray<BezierData>(eventCount, Allocator.TempJob);

            Dependency = new NewLineGetBezierJob
                         {
                             BezierData     = bezierData,
                             JoinPoints     = GetComponentDataFromEntity<LineJoinPoint>(),
                             LineTool       = GetSingleton<LineTool>(),
                             LineJoinPoints = lineJoinPoints
                         }.Schedule(eventCount, 4, Dependency);

            var boundsArray = new NativeArray<float3x2>(eventCount, Allocator.TempJob);

            Dependency = new NewLineGetBoundsFromBezierJob
                         {
                             BezierData  = bezierData,
                             BoundsArray = boundsArray
                         }.Schedule(eventCount, 4, Dependency);

            Dependency = new NewLineUpdateLineEntityJob
                         {
                             LineEntities   = lineEntities,
                             BoundsArray    = boundsArray,
                             Lines          = GetComponentDataFromEntity<Line>(true),
                             LineProfiles   = GetComponentDataFromEntity<LineProfile>(),
                             Ecb            = LineEndSimBufferSystem.Instance.CreateCommandBuffer().ToConcurrent(),
                             DefaultProfile = LineProfile.Default()
                         }.Schedule(eventCount, 4, Dependency);

            Dependency = new NewLineGenerateKnotsJob
                         {
                             BezierData       = bezierData,
                             HeightBezierData = heightBeziers,
                             KnotData         = GetBufferFromEntity<LineKnotData>(),
                             LineEntities     = lineEntities,
                             LineProfiles     = GetComponentDataFromEntity<LineProfile>(),
                             LineJoinPoints   = lineJoinPoints,
                             Lines            = GetComponentDataFromEntity<Line>()
                         }.Schedule(Dependency);

            Dependency = new NewLineGenerateMinMaxHeightMapJob
                         {
                             Ecb = LineEndSimBufferSystem.Instance.CreateCommandBuffer().ToConcurrent(),
                             LineEntities = lineEntities,
                             LineProfiles = GetComponentDataFromEntity<LineProfile>(true),
                             Lines = GetComponentDataFromEntity<Line>(true),
                             KnotData = GetBufferFromEntity<LineKnotData>(),
                             HeightMaps = GetBufferFromEntity<LineTerrainMinMaxHeightMap>(),
                             BoundsArray = boundsArray,
                             DefaultProfile = LineProfile.Default(),
                             TerrainSize2 = Terrain.activeTerrain.terrainData.size,
                             HeightMapResolution = Terrain.activeTerrain.terrainData.heightmapResolution

                         }.Schedule(eventCount, 4, Dependency);

            Dependency = new NewLineUpdateJoinPointsJob
                         {
                             Ecb            = LineEndSimBufferSystem.Instance.CreateCommandBuffer().ToConcurrent(),
                             LineEntities   = lineEntities,
                             Lines          = GetComponentDataFromEntity<Line>(),
                             LineJoinPoints = lineJoinPoints
                         }.Schedule(eventCount, 4, Dependency);

            Dependency = new LineSetDirtyJob
                         {
                             LineEntities = lineEntities,
                             Ecb          = LineEndSimBufferSystem.Instance.CreateCommandBuffer().ToConcurrent()
                         }.Schedule(eventCount, 4, Dependency);

            Dependency = new LineTriggerMeshRebuildJob
                         {
                             Ecb           = LineEndSimBufferSystem.Instance.CreateCommandBuffer().ToConcurrent(),
                             LineEntities  = lineEntities,
                             LineProfiles  = GetComponentDataFromEntity<LineProfile>(),
                             Lines         = GetComponentDataFromEntity<Line>(true),
                             DefaultPrefab = LineDefaultMeshBuilderSystem.Prefab
                         }.Schedule(eventCount, 4, Dependency);

            new DeallocateJob<Entity, JoinPointPair, NewLineUpdateEvent, float3x2>
            {
                NativeArray1 = lineEntities,
                NativeArray2 = lineJoinPoints,
                NativeArray3 = eventData,
                NativeArray4 = boundsArray
            }.Schedule(Dependency);

            new DeallocateJob<NewLine, float2x4, BezierData>
            {
                NativeArray1 = updatedNewLines,
                NativeArray2 = heightBeziers,
                NativeArray3 = bezierData
            }.Schedule(Dependency);

            LineEndSimBufferSystem.Instance.CreateCommandBuffer().DestroyEntity(eventQuery);

            LineEndSimBufferSystem.Instance.AddJobHandleForProducer(Dependency);
        }
    }
}