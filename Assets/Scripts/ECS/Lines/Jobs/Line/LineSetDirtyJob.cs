using Sibz.Lines.ECS.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Sibz.Lines.ECS.Jobs
{
    public struct LineSetDirtyJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Entity> LineEntities;
        public EntityCommandBuffer.Concurrent Ecb;

        public void Execute(int index)
        {
            if (LineEntities.IndexOf<Entity>(LineEntities[index]) != index)
            {
                return;
            }

            Ecb.AddComponent<Dirty>(index, LineEntities[index]);
        }
    }
}