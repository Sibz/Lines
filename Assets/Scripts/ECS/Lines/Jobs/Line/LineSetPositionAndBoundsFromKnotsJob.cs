using Sibz.Lines.ECS.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Sibz.Lines.ECS.Jobs
{
    public struct LineSetPositionAndBoundsFromKnotsJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<Entity> LineEntities;

        [ReadOnly]
        public BufferFromEntity<LineKnotData> KnotDataBuffer;

        [ReadOnly]
        public ComponentDataFromEntity<LineProfile> LineProfiles;

        [ReadOnly]
        public ComponentDataFromEntity<Line> Lines;

        public LineProfile                    DefaultProfile;
        public EntityCommandBuffer.Concurrent Ecb;

        public void Execute(int index)
        {
            var knotBuffer = KnotDataBuffer[LineEntities[index]];
            var min        = new float3(float.MaxValue, float.MaxValue, float.MaxValue);
            var max        = new float3();
            for (var i = 0; i < knotBuffer.Length; i++)
            {
                min.x = math.min(knotBuffer[i].Position.x, min.x);
                min.y = math.min(knotBuffer[i].Position.y, min.y);
                min.z = math.min(knotBuffer[i].Position.z, min.z);
                max.x = math.max(knotBuffer[i].Position.x, max.x);
                max.y = math.max(knotBuffer[i].Position.y, max.y);
                max.z = math.max(knotBuffer[i].Position.z, max.z);
            }

            var line = Lines[LineEntities[index]];
            line.Position = math.lerp(min, max, 0.5f);
            line.BoundingBoxSize =
                max - min +
                (LineProfiles.Exists(LineEntities[index])
                     ? LineProfiles[LineEntities[index]].Width * 2
                     : DefaultProfile.Width * 2);

            Ecb.SetComponent(index, LineEntities[index], line);
        }
    }
}