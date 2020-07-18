using Sibz.Lines.ECS.Components;
using Sibz.Lines.ECS.Events;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Sibz.Lines.ECS.Jobs
{
    [BurstCompile]
    public struct NewLineGetUpdatedJoinPoints : IJobParallelFor
    {
        //[ReadOnly] [DeallocateOnJobCompletion] public NativeArray<NewLineJoinPointUpdateSystem.JoinPointUpdateData> JoinData;
        [ReadOnly]
        public ComponentDataFromEntity<LineJoinPoint> JoinPoints;

        [ReadOnly]
        public NativeArray<NewLineUpdateEvent> EventData;

        [NativeDisableParallelForRestriction]
        public NativeArray<JoinPointPair> LineJoinPoints;

        [ReadOnly]
        public ComponentDataFromEntity<Line> Lines;

        public EntityCommandBuffer.Concurrent Ecb;

        public void Execute(int index)
        {
            var eventData = EventData[index];
            if (!eventData.UpdateJoinPoints) return;

            var joinData = LineJoinPoints[index];
            var line = Lines[eventData.LineEntity];

            // Are we trying to update this join to the same join
            // as the other join, AKA joining both ends to the same join point
            // This would be invalid so don't join in this case
            var thisJoinPoint = eventData.JoinPoint == line.JoinPointA
                                    ? joinData.A
                                    : joinData.B;
            var otherJoinPoint = eventData.JoinPoint == line.JoinPointA
                                     ? joinData.B
                                     : joinData.A;
            var otherEndIsJoinedToRequestedJoinToPoint =
                otherJoinPoint.JoinToPointEntity == eventData.JoinTo;

            thisJoinPoint.Pivot = eventData.Position;

            var newJoinPoint = JoinPoints.Exists(eventData.JoinTo)
                                   ? JoinPoints[eventData.JoinTo]
                                   : default;

            if (eventData.JoinTo != Entity.Null
             && !otherEndIsJoinedToRequestedJoinToPoint)
            {
                thisJoinPoint.Direction         = -newJoinPoint.Direction;
                thisJoinPoint.JoinToPointEntity = eventData.JoinTo;
                newJoinPoint.JoinToPointEntity  = eventData.JoinPoint;
                /*Ecb.SetComponent(index, eventData.JoinPoint, thisJoinPoint);
                Ecb.SetComponent(index, eventData.JoinTo, newJoinPoint);*/
            }
            else if (eventData.JoinTo == Entity.Null && thisJoinPoint.IsJoined)
            {
                // We assume here that no JoinTo data means we don't want to join
                // This should always be the case

                var joinedToPoint = JoinPoints[thisJoinPoint.JoinToPointEntity];
                LineJoinPoint.UnJoin(Ecb, index, ref thisJoinPoint, ref joinedToPoint);
            }
            /*else
            {
                //Ecb.SetComponent(index, eventData.JoinPoint, thisJoinPoint);
            }*/

            joinData.A = eventData.JoinPoint == line.JoinPointA
                                      ? thisJoinPoint
                                      : otherJoinPoint;
            joinData.B = eventData.JoinPoint == line.JoinPointA
                                      ? otherJoinPoint
                                      : thisJoinPoint;
            LineJoinPoints[index] = joinData;
        }
    }
}