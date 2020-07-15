using System;
using Sibz.Lines.ECS.Components;
using Sibz.Lines.ECS.Jobs;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

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
            var knotBuffers  = GetBufferFromEntity<LineKnotData>(true);
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
                             LineKnotData   = knotBuffers,
                             Ecb            = LineEndSimBufferSystem.Instance.CreateCommandBuffer().ToConcurrent()
                         }.Schedule(linesToCheck.Length, 4, Dependency);

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

            Dependency = new DeallocateJob<Entity>
                         {
                             NativeArray1 = linesToCheck
                         }.Schedule(Dependency);

            LineEndSimBufferSystem.Instance.AddJobHandleForProducer(Dependency);
        }
    }
}