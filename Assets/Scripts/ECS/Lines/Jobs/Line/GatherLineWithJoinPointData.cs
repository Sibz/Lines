using System;
using Sibz.Lines.ECS.Components;
using Sibz.Lines.ECS.Events;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Sibz.Lines.ECS.Jobs
{
    [BurstCompile]
    public struct GatherLineWithJoinPointData : IJobParallelFor
    {
        [ReadOnly]
        public ComponentDataFromEntity<Line> Lines;

        [ReadOnly]
        public ComponentDataFromEntity<LineJoinPoint> JoinPoints;

        [ReadOnly]
        public NativeArray<NewLineUpdateEvent> EventData;

        [WriteOnly, NativeDisableParallelForRestriction]
        public NativeArray<LineWithJoinPointData> LineWithJoinData;

        [WriteOnly, NativeDisableParallelForRestriction]
        public NativeArray<Entity> LineEntities;

        public void Execute(int i)
        {
            var lineEntity = EventData[i].LineEntity;
            if (!Lines.Exists(lineEntity)) return;

            LineEntities[i] = lineEntity;
            var line = Lines[lineEntity];
            LineWithJoinData[i] = new LineWithJoinPointData
                                  {
                                      Line = line,
                                      JoinPointA =
                                          JoinPoints.Exists(line.JoinPointA) // It should exist, but just in case
                                              ? JoinPoints[line.JoinPointA]
                                              : throw new InvalidOperationException("JoinPointA didn't exist"),
                                      JoinPointB =
                                          JoinPoints.Exists(line.JoinPointB) // It should exist, but just in case
                                              ? JoinPoints[line.JoinPointB]
                                              : throw new InvalidOperationException("JoinPointB didn't exist")
                                  };
        }
    }
}