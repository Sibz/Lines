using System;
using Sibz.Lines.ECS.Components;
using Sibz.Lines.ECS.Jobs;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Sibz.Lines.ECS.Systems
{
    public class LineMergeSystem : SystemBase
    {
        private EntityQuery mergeCheckQuery;
        private HeightMapsJobs heightMapsJobs;

        protected override void OnCreate()
        {
            mergeCheckQuery = GetEntityQuery(typeof(Line), typeof(MergeCheck));
            RequireForUpdate(mergeCheckQuery);
        }

        protected override void OnUpdate()
        {
            var joinPoints   = GetComponentDataFromEntity<LineJoinPoint>(true);
            var lines        = GetComponentDataFromEntity<Line>();
            var knotBuffers  = GetBufferFromEntity<LineKnotData>();
            var linesToCheck = mergeCheckQuery.ToEntityArray(Allocator.TempJob);
            Dependency.Complete();
            LineEndSimBufferSystem.Instance
                                  .CreateCommandBuffer()
                                  .RemoveComponent<MergeCheck>(mergeCheckQuery);

            Dependency = new LineMergeJob
                         {
                             Lines          = lines,
                             LineEntities   = linesToCheck,
                             LineJoinPoints = joinPoints,
                             LineProfiles   = GetComponentDataFromEntity<LineProfile>(),
                             LineKnotData   = knotBuffers,
                             Ecb            = LineEndSimBufferSystem.Instance.CreateCommandBuffer().ToConcurrent(),
                             DefaultProfile = LineProfile.Default()
                         }.Schedule(linesToCheck.Length, 4, Dependency);

            // Todo make LineMergeJob update Lines direct without Ecb
            var boundsArray = new NativeArray<float3x2>(linesToCheck.Length, Allocator.TempJob);
            Dependency = new LineGetBoundsJob
                         {
                             Entities = linesToCheck,
                             Lines    = GetComponentDataFromEntity<Line>(),
                             Bounds   = boundsArray
                         }.Schedule(linesToCheck.Length, 4, Dependency);
            heightMapsJobs.Dispose();
            heightMapsJobs = new HeightMapsJobs
                             {
                                 Entities               = linesToCheck,
                                 Lines                  = lines,
                                 KnotData               = knotBuffers,
                                 LineProfilesFromEntity = GetComponentDataFromEntity<LineProfile>(),
                                 HeightMapBuffers       = GetBufferFromEntity<LineTerrainMinMaxHeightMap>(),
                                 BoundsArray            = boundsArray,
                                 TerrainSize            = Terrain.activeTerrain.terrainData.size,
                                 HeightMapResolution    = Terrain.activeTerrain.terrainData.heightmapResolution
                             };
            Dependency = heightMapsJobs.Schedule(Dependency);

            // TODO only trigger this when lines are merged
            Dependency = new LineTriggerMeshRebuildJob
                         {
                             Ecb           = LineEndSimBufferSystem.Instance.CreateCommandBuffer().ToConcurrent(),
                             Lines         = GetComponentDataFromEntity<Line>(true),
                             LineProfiles  = GetComponentDataFromEntity<LineProfile>(true),
                             LineEntities  = linesToCheck,
                             DefaultPrefab = LineDefaultMeshBuilderSystem.Prefab
                         }.Schedule(linesToCheck.Length, 4, Dependency);

            Dependency = new LineSetDirtyJob
                         {
                             Ecb          = LineEndSimBufferSystem.Instance.CreateCommandBuffer().ToConcurrent(),
                             LineEntities = linesToCheck
                         }.Schedule(linesToCheck.Length, 4, Dependency);

            Dependency = new DeallocateJob<Entity, float3x2>
                         {
                             NativeArray1 = linesToCheck,
                             NativeArray2 = boundsArray
                         }.Schedule(Dependency);

            LineEndSimBufferSystem.Instance.AddJobHandleForProducer(Dependency);
        }

        protected override void OnStopRunning()
        {
            heightMapsJobs.Dispose();
        }
    }
}