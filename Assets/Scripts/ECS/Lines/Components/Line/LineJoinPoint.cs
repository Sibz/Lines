using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Sibz.Lines.ECS.Components
{
    public struct LineJoinPoint : IComponentData
    {
        public Entity ParentEntity;
        public Entity JoinToPointEntity;
        public float3 Pivot;
        public float3 Direction;
        public float DistanceFromPivot;

        // TODO: We need to ensure orphaned joins are cleaned up
        public bool IsJoined => !JoinToPointEntity.Equals(Entity.Null);

        /// <summary>
        /// Max movement from direction in radians
        /// </summary>
        public float AngularLimit;

        private const float DefaultAngularLimit = 6.283185307179586476925286766559f;

        public static Entity New(Entity parentEntity, float3 pivot, float3 direction = default,
            float distanceFromPivot = 0, float angularLimit = DefaultAngularLimit)
        {
            Entity entity = LineWorld.Em.CreateEntity(typeof(LineJoinPoint), typeof(JoinEditable));
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
            ref LineJoinPoint fromData, ref LineJoinPoint toData)
        {
            // TODO: Check isn't already un-joined
            var fromEntity = toData.JoinToPointEntity;
            var toEntity = fromData.JoinToPointEntity;
            fromData.JoinToPointEntity = Entity.Null;
            toData.JoinToPointEntity = Entity.Null;
            if (fromEntity != Entity.Null)
                ecb.SetComponent(jobIndex, fromEntity, fromData);
            if (toEntity != Entity.Null)
                ecb.SetComponent(jobIndex, toEntity, toData);
        }

        public static void UnJoin(EntityCommandBuffer ecb,
            ref LineJoinPoint fromData, Entity fromEntity,
            ref LineJoinPoint toData, Entity toEntity)
        {
            fromData.JoinToPointEntity = Entity.Null;
            toData.JoinToPointEntity = Entity.Null;
            ecb.SetComponent(fromEntity, fromData);
            ecb.SetComponent(toEntity, toData);
        }

        public static void Join(Entity a, Entity b)
        {
            static void Join(Entity a1, Entity b1)
            {
                LineJoinPoint jp = LineWorld.Em.GetComponentData<LineJoinPoint>(a1);
                jp.JoinToPointEntity = b1;
                LineWorld.Em.SetComponentData(a1, jp);
            }

            Join(a, b);
            Join(b, a);
        }

        public static void Join(ref LineJoinPoint jpA, ref LineJoinPoint jpB)
        {
            static void Join(ref LineJoinPoint a1, ref LineJoinPoint b1)
            {
                a1.JoinToPointEntity = b1.JoinToPointEntity;
                LineWorld.Em.SetComponentData(b1.JoinToPointEntity, a1);
            }

            Join(ref jpA, ref jpB);
            Join(ref jpB, ref jpA);
        }

        public static void Join(
            EntityCommandBuffer.Concurrent ecb, int jobIndex,
            ref LineJoinPoint fromData, Entity fromEntity,
            ref LineJoinPoint toData, Entity toEntity)
        {
            void Join(Entity a1, ref LineJoinPoint data, Entity b1)
            {
                data.JoinToPointEntity = b1;
                ecb.SetComponent(jobIndex, a1, data);
            }

            Join(fromEntity, ref fromData, toEntity);
            Join(toEntity, ref toData, fromEntity);
        }

        public static void Join(
            EntityCommandBuffer ecb,
            ref LineJoinPoint fromData, Entity fromEntity,
            ref LineJoinPoint toData, Entity toEntity)
        {
            void Join(Entity a1, ref LineJoinPoint data, Entity b1)
            {
                data.JoinToPointEntity = b1;
                ecb.SetComponent(a1, data);
            }

            Join(fromEntity, ref fromData, toEntity);
            Join(toEntity, ref toData, fromEntity);
        }
    }
}