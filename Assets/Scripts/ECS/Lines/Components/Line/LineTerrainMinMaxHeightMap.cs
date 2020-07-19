using Unity.Entities;
using Unity.Mathematics;

namespace Sibz.Lines.ECS.Components
{
    public struct LineTerrainMinMaxHeightMap : IBufferElementData
    {
        public int2 Position;
        public float Min;
        public float Max;
    }
}