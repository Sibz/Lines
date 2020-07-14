using Sibz.Lines.ECS.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Sibz.Lines.ECS.Jobs
{
    public struct LineTriggerMeshRebuildJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Entity> LineEntities;
        [ReadOnly] public NativeArray<LineWithJoinPointData> LineWithJoinData;
        [ReadOnly] public ComponentDataFromEntity<LineProfile> LineProfiles;
        public EntityCommandBuffer.Concurrent Ecb;
        public Entity DefaultPrefab;

        public void Execute(int index)
        {
            if (LineEntities.IndexOf<Entity>(LineEntities[index]) != index)
            {
                return;
            }

            MeshBuildData buildData = new MeshBuildData
            {
                LineEntity = LineEntities[index]
            };

            Entity meshBuildTriggerEntity = Ecb.Instantiate(index,
                LineProfiles.Exists(LineWithJoinData[index].Line.Profile)
                    ? LineProfiles[LineWithJoinData[index].Line.Profile].MeshBuildPrefab
                    : DefaultPrefab);

            Ecb.SetComponent(index,meshBuildTriggerEntity, buildData);
        }
    }
}