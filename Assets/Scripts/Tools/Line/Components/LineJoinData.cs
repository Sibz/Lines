using Unity.Entities;

namespace Sibz.Lines
{
    public struct LineJoinData : IComponentData
    {
        public int ConnectedIndex;
        public Entity ConnectedEntity;
        public bool IsConnected => !ConnectedEntity.Equals(Entity.Null);
    }
}