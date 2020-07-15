using Sibz.Lines.ECS.Components;
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
            // Our first check is to get the join points of the check line, and see if they belong
            // to a line, if so we can queue the first for merging
            // we always merge into the other line
            // we only merge one, but then add a merge check to that line
            var joinPoints = GetComponentDataFromEntity<LineJoinPoint>();
            var lines = GetComponentDataFromEntity<Line>();
            var linesToCheck = mergeCheckQuery.ToEntityArray(Allocator.TempJob);
            Dependency.Complete();
            EntityManager.RemoveComponent<MergeCheck>(mergeCheckQuery);

        }

        public struct LineMergeCheckJob : IJobParallelFor
        {
            public ComponentDataFromEntity<LineJoinPoint> LineJoinPoints;
            public ComponentDataFromEntity<Line> Lines;
            public NativeArray<Entity> LineEntities;

            public void Execute(int index)
            {

            }
        }
    }
}