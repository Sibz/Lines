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

        protected override void OnCreate()
        {
            mergeCheckQuery = GetEntityQuery(typeof(Line), typeof(MergeCheck));
            RequireForUpdate(mergeCheckQuery);
        }

        protected override void OnUpdate()
        {
            var joinPoints   = GetComponentDataFromEntity<LineJoinPoint>(true);
            var lines        = GetComponentDataFromEntity<Line>(true);
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
                             LineProfiles = GetComponentDataFromEntity<LineProfile>(),
                             LineKnotData   = knotBuffers,
                             Ecb            = LineEndSimBufferSystem.Instance.CreateCommandBuffer().ToConcurrent(),
                             DefaultProfile = LineProfile.Default()
                         }.Schedule(linesToCheck.Length, 4, Dependency);

            var boundsArray = new NativeArray<float3x2>(linesToCheck.Length, Allocator.TempJob);
            Dependency = new NewLineGenerateMinMaxHeightMapJob
                         {
                             Ecb                 = LineEndSimBufferSystem.Instance.CreateCommandBuffer().ToConcurrent(),
                             LineEntities        = linesToCheck,
                             LineProfiles        = GetComponentDataFromEntity<LineProfile>(true),
                             Lines               = GetComponentDataFromEntity<Line>(true),
                             KnotData            = GetBufferFromEntity<LineKnotData>(true),
                             HeightMaps          = GetBufferFromEntity<LineTerrainMinMaxHeightMap>(),
                             BoundsArray         = boundsArray,
                             DefaultProfile      = LineProfile.Default(),
                             TerrainSize2        = Terrain.activeTerrain.terrainData.size,
                             HeightMapResolution = Terrain.activeTerrain.terrainData.heightmapResolution

                         }.Schedule(linesToCheck.Length, 4, Dependency);

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
    }
}