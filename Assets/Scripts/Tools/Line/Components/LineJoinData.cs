using System;
using Unity.Entities;

namespace Sibz.Lines
{
    [Serializable]
    public struct LineJoinData : IComponentData
    {
        public int ConnectedIndex;
        public Entity ConnectedEntity;
        public bool IsConnected => !ConnectedEntity.Equals(Entity.Null);
    }
}