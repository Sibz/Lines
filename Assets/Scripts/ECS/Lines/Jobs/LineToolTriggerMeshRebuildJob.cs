using Sibz.Lines.ECS.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Sibz.Lines.ECS.Jobs
{
    public struct LineToolTriggerMeshRebuildJob : IJob
    {
        [ReadOnly, DeallocateOnJobCompletion]
        public NativeArray<bool> DidChange;

        public EntityCommandBuffer Ecb;
        public Entity              LineEntity;
        public Entity              MeshBuilderPrefab;

        public void Execute()
        {
            if (!DidChange[0]) return;

            var buildData = new MeshBuildData
                            {
                                LineEntity = LineEntity
                            };

            var meshBuildTriggerEntity = Ecb.Instantiate(MeshBuilderPrefab);
            Ecb.SetComponent(meshBuildTriggerEntity, buildData);
            Ecb.AddComponent<MeshUpdated>(LineEntity);
        }
    }
}