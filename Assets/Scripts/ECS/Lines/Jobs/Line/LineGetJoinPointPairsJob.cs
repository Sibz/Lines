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
    public struct LineGetJoinPointPairsJob : IJobParallelFor
    {
        [ReadOnly]
        public ComponentDataFromEntity<Line> Lines;

        [ReadOnly]
        public ComponentDataFromEntity<LineJoinPoint> JoinPoints;

        [ReadOnly]
        public NativeArray<NewLineUpdateEvent> EventData;

        [WriteOnly, NativeDisableParallelForRestriction]
        public NativeArray<Entity> LineEntities;

        [WriteOnly, NativeDisableParallelForRestriction]
        public NativeArray<JoinPointPair> LineJoinPoints;

        public void Execute(int i)
        {
            var lineEntity = EventData[i].LineEntity;
            if (!Lines.HasComponent(lineEntity)) return;

            LineEntities[i] = lineEntity;
            var line = Lines[lineEntity];
            LineJoinPoints[i] = new JoinPointPair
                                {
                                    A =
                                        JoinPoints.HasComponent(line.JoinPointA) // It should exist, but just in case
                                            ? JoinPoints[line.JoinPointA]
                                            : throw new InvalidOperationException("JoinPointA didn't exist"),
                                    B =
                                        JoinPoints.HasComponent(line.JoinPointB) // It should exist, but just in case
                                            ? JoinPoints[line.JoinPointB]
                                            : throw new InvalidOperationException("JoinPointB didn't exist")
                                };
        }
    }
}