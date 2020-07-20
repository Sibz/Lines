using System;
using Sibz.Lines.ECS.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Sibz.Lines.ECS.Jobs
{
    [BurstCompile]
    public struct LineMergeJob : IJobParallelFor
    {
        [ReadOnly]
        public ComponentDataFromEntity<LineJoinPoint> LineJoinPoints;

        [ReadOnly]
        public ComponentDataFromEntity<Line> Lines;

        [ReadOnly]
        public ComponentDataFromEntity<LineProfile> LineProfiles;

        [NativeDisableParallelForRestriction]
        public BufferFromEntity<LineKnotData> LineKnotData;

        public NativeArray<Entity>            LineEntities;
        public EntityCommandBuffer.Concurrent Ecb;
        public LineProfile                    DefaultProfile;

        private Line   line;
        private Entity lineEntity;
        private int    index;

        public void Execute(int i)
        {
            index      = i;
            line       = Lines[LineEntities[index]];
            lineEntity = LineEntities[index];

            if (!LineJoinPoints.Exists(line.JoinPointA) || !LineJoinPoints.Exists(line.JoinPointB))
                throw new InvalidOperationException("One or more line join points didn't exist");

            Line   otherLine;
            Entity thisJoinEntity;
            Entity otherLineEntity;
            var    joinA = LineJoinPoints[line.JoinPointA];
            var    joinB = LineJoinPoints[line.JoinPointB];

            if (joinA.IsJoined && Lines.Exists(LineJoinPoints[joinA.JoinToPointEntity].ParentEntity))
            {
                otherLineEntity = LineJoinPoints[joinA.JoinToPointEntity].ParentEntity;
                otherLine       = Lines[otherLineEntity];
                thisJoinEntity  = line.JoinPointA;
            }
            else if (joinB.IsJoined && Lines.Exists(LineJoinPoints[joinB.JoinToPointEntity].ParentEntity))
            {
                otherLineEntity = LineJoinPoints[joinB.JoinToPointEntity].ParentEntity;
                otherLine       = Lines[otherLineEntity];
                thisJoinEntity  = line.JoinPointB;
            }
            else
            {
                return;
            }

            // Connecting to our self
            if (otherLineEntity == lineEntity) return;

            // Recheck this line in case other join is joined too
            Ecb.AddComponent<MergeCheck>(index, lineEntity);
            // Schedule destruction of other line mwuhahahaha
            Ecb.AddComponent<Destroy>(index, otherLineEntity);

            var thisJoin        = LineJoinPoints[thisJoinEntity];
            var otherJoinEntity = thisJoin.JoinToPointEntity;
            var joinState       = GetJoinState(thisJoinEntity, otherJoinEntity, otherLine);

            MergeFrom(joinState,
                      otherLineEntity);

            UpdatePositionAndBounds();

            UpdateJoinPoints(thisJoinEntity, joinState, otherLine);

            Ecb.SetComponent(index, LineEntities[index], line);
        }

        private void UpdatePositionAndBounds()
        {
            var knotBuffer = LineKnotData[lineEntity];
            var min = new float3(float.MaxValue, float.MaxValue, float.MaxValue);
            var max = new float3(float.MinValue, float.MinValue, float.MinValue);
            for (var i = 0; i < knotBuffer.Length; i++)
            {
                min.x = math.min(knotBuffer[i].Position.x, min.x);
                min.y = math.min(knotBuffer[i].Position.y, min.y);
                min.z = math.min(knotBuffer[i].Position.z, min.z);
                max.x = math.max(knotBuffer[i].Position.x, max.x);
                max.y = math.max(knotBuffer[i].Position.y, max.y);
                max.z = math.max(knotBuffer[i].Position.z, max.z);
            }

            line.Position = math.lerp(min, max, 0.5f);
            line.BoundingBoxSize =
                max - min +
                (LineProfiles.Exists(LineEntities[index])
                     ? LineProfiles[LineEntities[index]].Width * 2
                     : DefaultProfile.Width * 2);
        }

        private void UpdateJoinPoints(Entity thisJoinEntity, JoinState joinState, Line otherLine)
        {
            var otherLineNonMergedJointPoint = LineJoinPoints[joinState == JoinState.AtoA ||
                                                              joinState == JoinState.BtoA
                                                                  ? otherLine.JoinPointB
                                                                  : otherLine.JoinPointA];

            // Un-join other line (it will be destroyed)
            // TODO Check back here as this may be done on destroy other line
            LineJoinPoint.UnJoinIfJoined(Ecb, index, LineJoinPoints, otherLine.JoinPointA);
            LineJoinPoint.UnJoinIfJoined(Ecb, index, LineJoinPoints, otherLine.JoinPointB);

            // Mirror other join
            var thisJoin = otherLineNonMergedJointPoint;
            // Except parent entity
            thisJoin.ParentEntity = lineEntity;
            if (thisJoin.IsJoined)
                LineJoinPoint.Join(Ecb, index, LineJoinPoints,
                                   thisJoinEntity,
                                   thisJoin.JoinToPointEntity,
                                   true, thisJoin);
            else
                Ecb.SetComponent(index, thisJoinEntity, thisJoin);
        }

        private JoinState GetJoinState(Entity thisJoinEntity, Entity otherJoinEntity, Line otherLine)
        {
            var isThisJoinA = line.JoinPointA.Equals(thisJoinEntity);
            var isFromJoinA = otherLine.JoinPointA.Equals(otherJoinEntity);
            if (!isThisJoinA && isFromJoinA) return JoinState.BtoA;
            if (!isThisJoinA) return JoinState.BtoB;
            if (!isFromJoinA) return JoinState.AtoB;
            return JoinState.AtoA;
        }

        private enum JoinState
        {
            AtoB,
            AtoA,
            BtoA,
            BtoB
        }

        private void MergeFrom(JoinState                       joinState,
                               Entity                          otherLineEntity)
        {
            var thisKnots  = LineKnotData[lineEntity];
            var thisKnotArray = thisKnots.ToNativeArray(Allocator.Temp);
            var otherKnots = LineKnotData[otherLineEntity];
            var otherKnotsArray = otherKnots.ToNativeArray(Allocator.Temp);
            //var newKnots   = Ecb.SetBuffer<LineKnotData>(index, lineEntity);
            thisKnots.Clear();

            void AddKnotsInSameOrder(NativeArray<LineKnotData> knots, bool skipFirst = false)
            {
                var len = knots.Length;
                for (var i = skipFirst ? 1 : 0; i < len; i++)
                    thisKnots.Add(knots[i]);
            }

            void AddKnotsInReverseOrder(NativeArray<LineKnotData> knots, bool skipFirst = false)
            {
                var len = knots.Length;
                for (var i = len - (skipFirst ? 2 : 1); i >= 0; i--) // -2 as to skip the first knot
                    thisKnots.Add(knots[i]);
            }

            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (joinState)
            {
                case JoinState.BtoA:
                    AddKnotsInSameOrder(thisKnotArray);
                    AddKnotsInSameOrder(otherKnotsArray, true);
                    break;
                case JoinState.BtoB:
                    AddKnotsInSameOrder(thisKnotArray);
                    AddKnotsInReverseOrder(otherKnotsArray, true);
                    break;
                case JoinState.AtoB:
                    AddKnotsInSameOrder(otherKnotsArray);
                    AddKnotsInSameOrder(thisKnotArray, true);
                    break;
                case JoinState.AtoA:
                    AddKnotsInReverseOrder(otherKnotsArray);
                    AddKnotsInSameOrder(thisKnotArray, true);
                    break;
            }


        }
    }
}