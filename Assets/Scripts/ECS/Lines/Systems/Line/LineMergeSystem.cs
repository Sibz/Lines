using Sibz.Lines.ECS.Components;
using Sibz.Lines.ECS.Jobs;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Sibz.Lines.ECS.Systems
{
    public class LineMergeSystem : SystemBase
    {
        private EntityQuery mergeCheckQuery;

        protected override void OnCreate()
        {
            mergeCheckQuery = GetEntityQuery(typeof(Line), typeof(MergeCheck));
            RequireForUpdate(mergeCheckQuery);
        }

        protected override void OnUpdate()
        {
            var joinPoints   = GetComponentDataFromEntity<LineJoinPoint>(true);
            var lines        = GetComponentDataFromEntity<Line>(true);
            var knotBuffers  = GetBufferFromEntity<LineKnotData>(true);
            var linesToCheck = mergeCheckQuery.ToEntityArray(Allocator.TempJob);
            Dependency.Complete();
            LineEndSimBufferSystem.Instance
                                  .CreateCommandBuffer()
                                  .RemoveComponent<MergeCheck>(mergeCheckQuery);

            Dependency = new LineMergeJob
                         {
                             Lines          = lines,
                             LineEntities   = linesToCheck,
                             LineJoinPoints = joinPoints,
                             LineKnotData   = knotBuffers,
                             Ecb            = LineEndSimBufferSystem.Instance.CreateCommandBuffer().ToConcurrent()
                         }.Schedule(linesToCheck.Length, 4, Dependency);

            Dependency = new LineTriggerMeshRebuildJob
                         {
                             Ecb           = LineEndSimBufferSystem.Instance.CreateCommandBuffer().ToConcurrent(),
                             Lines         = GetComponentDataFromEntity<Line>(true),
                             LineProfiles  = GetComponentDataFromEntity<LineProfile>(true),
                             LineEntities  = linesToCheck,
                             DefaultPrefab = LineDefaultMeshBuilderSystem.Prefab
                         }.Schedule(linesToCheck.Length, 4, Dependency);

            Dependency = new LineSetDirtyJob
                         {
                             Ecb          = LineEndSimBufferSystem.Instance.CreateCommandBuffer().ToConcurrent(),
                             LineEntities = linesToCheck
                         }.Schedule(linesToCheck.Length, 4, Dependency);

            Dependency = new DeallocateJob<Entity>
                         {
                             NativeArray1 = linesToCheck
                         }.Schedule(Dependency);

            LineEndSimBufferSystem.Instance.AddJobHandleForProducer(Dependency);
        }

        public struct LineMergeJob : IJobParallelFor
        {
            [ReadOnly]
            public ComponentDataFromEntity<LineJoinPoint> LineJoinPoints;

            [ReadOnly]
            public ComponentDataFromEntity<Line> Lines;

            [ReadOnly]
            public BufferFromEntity<LineKnotData> LineKnotData;

            public NativeArray<Entity>            LineEntities;
            public EntityCommandBuffer.Concurrent Ecb;

            private Line   line;
            private Entity lineEntity;
            private int    index;

            public void Execute(int i)
            {
                index      = i;
                line       = Lines[LineEntities[index]];
                lineEntity = LineEntities[index];

                if (!LineJoinPoints.Exists(line.JoinPointA) || !LineJoinPoints.Exists(line.JoinPointB))
                    /* TODO see how we throw errors from burst jobs, if only when in editor
                         #if ENABLE_UNITY_COLLECTIONS_CHECKS
                        throw new InvalidOperationException("One or more line join points didn't exit");*/
                    return;

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

                // Connecting to ourself
                if (otherLineEntity == lineEntity)
                {
                    Debug.LogWarning("Tried to connect to ourself");
                    return;
                }

                // Recheck this line in case other join is joined too
                Ecb.AddComponent<MergeCheck>(index, lineEntity);
                // Schedule destruction of other line mwuhahahaha
                Ecb.AddComponent<Destroy>(index, otherLineEntity);

                var thisJoin        = LineJoinPoints[thisJoinEntity];
                var otherJoinEntity = thisJoin.JoinToPointEntity;
                var joinState       = GetJoinState(thisJoinEntity, otherJoinEntity, otherLine);

                MergeFrom(joinState, otherLineEntity, otherJoinEntity);

                UpdateJoinPoints(thisJoinEntity, joinState, otherLine);

                // TODO Destroy other line
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

            private DynamicBuffer<LineKnotData> MergeFrom(JoinState joinState,
                                                          Entity    otherLineEntity,
                                                          Entity    otherJoinEntity)
            {
                var thisKnots  = LineKnotData[lineEntity];
                var otherKnots = LineKnotData[otherLineEntity];
                var newKnots   = Ecb.SetBuffer<LineKnotData>(index, lineEntity);

                void AddKnotsInSameOrder(DynamicBuffer<LineKnotData> knots, bool skipFirst = false)
                {
                    var len = knots.Length;
                    for (var i = skipFirst ? 1 : 0; i < len; i++)
                        newKnots.Add(knots[i]);
                }

                void AddKnotsInReverseOrder(DynamicBuffer<LineKnotData> knots, bool skipFirst = false)
                {
                    var len = knots.Length;
                    for (var i = len - (skipFirst ? 2 : 1); i >= 0; i--) // -2 as to skip the first knot
                        newKnots.Add(knots[i]);
                }

                // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
                switch (joinState)
                {
                    case JoinState.BtoA:
                        AddKnotsInSameOrder(thisKnots);
                        AddKnotsInSameOrder(otherKnots, true);
                        break;
                    case JoinState.BtoB:
                        AddKnotsInSameOrder(thisKnots);
                        AddKnotsInReverseOrder(otherKnots, true);
                        break;
                    case JoinState.AtoB:
                        AddKnotsInSameOrder(otherKnots);
                        AddKnotsInSameOrder(thisKnots, true);
                        break;
                    case JoinState.AtoA:
                        AddKnotsInReverseOrder(otherKnots);
                        AddKnotsInSameOrder(thisKnots, true);
                        break;
                }

                return newKnots;
            }
        }
    }
}