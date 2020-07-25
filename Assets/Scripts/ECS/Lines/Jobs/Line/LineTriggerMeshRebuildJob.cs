using Sibz.Lines.ECS.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Sibz.Lines.ECS.Jobs
{
    [BurstCompile]
    public struct LineTriggerMeshRebuildJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<Entity> LineEntities;

        [ReadOnly]
        public ComponentDataFromEntity<Line> Lines;

        [ReadOnly]
        public ComponentDataFromEntity<LineProfile> LineProfiles;

        public EntityCommandBuffer.ParallelWriter Ecb;
        public Entity                         DefaultPrefab;

        public void Execute(int index)
        {
            if (LineEntities.IndexOf<Entity>(LineEntities[index]) != index) return;

            var buildData = new MeshBuildData
                            {
                                LineEntity = LineEntities[index]
                            };

            var meshBuildTriggerEntity = Ecb.Instantiate(index,
                                                         LineProfiles.HasComponent(Lines[LineEntities[index]].Profile)
                                                             ? LineProfiles[Lines[LineEntities[index]].Profile]
                                                                .MeshBuildPrefab
                                                             : DefaultPrefab);

            Ecb.SetComponent(index, meshBuildTriggerEntity, buildData);
        }
    }
}