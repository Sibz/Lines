using Sibz.Lines.ECS.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Sibz.Lines.ECS.Jobs
{
    public struct LineToolTriggerMeshRebuildJob : IJob
    {


        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<bool> DidChange;
        public EntityCommandBuffer.Concurrent Ecb;
        public int JobIndex;
        public Entity LineEntity;
        public Entity MeshBuilderPrefab;

        public void Execute()
        {
            if (!DidChange[0])
            {
                return;
            }

            MeshBuildData buildData = new MeshBuildData
            {
                LineEntity = LineEntity
            };

            Entity meshBuildTriggerEntity = Ecb.Instantiate(JobIndex, MeshBuilderPrefab);
            Ecb.SetComponent(JobIndex, meshBuildTriggerEntity, buildData);

        }
    }
}