using Sibz.Lines.ECS.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Sibz.Lines.ECS.Jobs
{
    public struct NewLineUpdateLineEntityJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<Entity> LineEntities;

        [ReadOnly]
        public ComponentDataFromEntity<Line> Lines;

        [ReadOnly]
        public ComponentDataFromEntity<LineProfile> LineProfiles;

        [ReadOnly]
        public NativeArray<float3x2> Bounds;

        public LineProfile                    DefaultProfile;
        public EntityCommandBuffer.Concurrent Ecb;

        public void Execute(int index)
        {
            var line = Lines[LineEntities[index]];
            line.Position = Bounds[index].c0;
            line.BoundingBoxSize =
                Bounds[index].c1 +
                (LineProfiles.Exists(LineEntities[index])
                     ? LineProfiles[LineEntities[index]].Width * 2
                     : DefaultProfile.Width * 2);
            Ecb.SetComponent(index, LineEntities[index], line);
        }
    }
}