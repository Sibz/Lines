using Unity.Entities;

namespace Sibz.Lines.ECS.Components
{
    public struct RemoveHeightMap : IComponentData
    {
        public Entity HeightMapOwner;
    }
}