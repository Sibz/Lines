using Unity.Entities;
using Unity.Mathematics;

namespace Sibz.Lines.ECS.Components
{
    public struct LineJoinPoint : IComponentData
    {
        public Entity ParentEntity;
        public Entity JoinedEntity;
        public float3 Pivot;
        public float3 Direction;
        public float3 DistanceFromPivot;
        /// <summary>
        /// Max movement from direction in radians
        /// </summary>
        public float AngularLimit;
    }
}