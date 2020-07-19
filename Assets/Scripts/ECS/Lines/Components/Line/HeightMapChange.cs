using Unity.Entities;
using Unity.Mathematics;

namespace Sibz.Lines.ECS.Components
{
    public struct HeightMapChange :IComponentData
    {
        public int2 StartPosition;
        public int2 Size;
    }
}