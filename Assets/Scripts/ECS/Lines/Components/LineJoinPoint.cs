using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Sibz.Lines.ECS.Components
{
    public struct LineJoinPoint : IComponentData
    {
        public Entity ParentEntity;
        public Entity JoinedEntity;
        public float3 Pivot;
        public float3 Direction;
        public float DistanceFromPivot;

        /// <summary>
        /// Max movement from direction in radians
        /// </summary>
        public float AngularLimit;

        private const float DefaultAngularLimit = 6.283185307179586476925286766559f;

        public static Entity New(Entity parentEntity, float3 pivot, float3 direction = default,
            float distanceFromPivot = 0, float angularLimit = DefaultAngularLimit)
        {
            Entity entity = LineWorld.Em.CreateEntity(typeof(LineJoinPoint));
            LineWorld.Em.SetComponentData(entity, new LineJoinPoint
            {
                ParentEntity = parentEntity,
                Pivot = pivot,
                Direction = direction,
                DistanceFromPivot = distanceFromPivot,
                AngularLimit = angularLimit
            });
            return entity;
        }

        public static void UnJoin(EntityCommandBuffer.Concurrent ecb, int jobIndex,
            ref LineJoinPoint fromData, Entity fromEntity,
            ref LineJoinPoint toData, Entity toEntity)
        {
            fromData.JoinedEntity = Entity.Null;
            toData.JoinedEntity = Entity.Null;
            ecb.SetComponent(jobIndex, fromEntity, fromData);
            ecb.SetComponent(jobIndex, toEntity, toData);
        }

        public static void Join(Entity a, Entity b)
        {
            static void Join(Entity a1, Entity b1)
            {
                LineJoinPoint jp = LineWorld.Em.GetComponentData<LineJoinPoint>(a1);
                jp.JoinedEntity = b1;
                LineWorld.Em.SetComponentData(a1, jp);
            }

            Join(a, b);
            Join(b, a);
        }

        public static void Join(
            EntityCommandBuffer.Concurrent ecb, int jobIndex,
            ref LineJoinPoint fromData, Entity fromEntity,
            ref LineJoinPoint toData, Entity toEntity)
        {
            void Join(Entity a1, ref LineJoinPoint data, Entity b1)
            {
                data.JoinedEntity = b1;
                ecb.SetComponent(jobIndex, a1, data);
            }

            Join(fromEntity, ref fromData, toEntity);
            Join(toEntity, ref toData, fromEntity);
        }
    }
}