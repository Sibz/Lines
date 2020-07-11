using Sibz.Lines.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Sibz.Lines
{
    [UpdateInGroup(typeof(LineSystemGroup))]
    public class LineUpdateSystem : SystemBase
    {
        private EntityQuery sectionsQuery;

        protected override void OnCreate()
        {
            sectionsQuery = GetEntityQuery(typeof(LineSection));
        }

        protected override void OnUpdate()
        {
            LineTool2 lineTool = GetSingleton<LineTool2>();
            Entity lineToolEntity = GetSingletonEntity<LineTool2>();

            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
            EntityCommandBuffer.Concurrent ecbConcurrent = ecb.ToConcurrent();

            NativeArray<Entity> lineSectionEntities =
                sectionsQuery.ToEntityArrayAsync(Allocator.TempJob, out JobHandle jh);
            NativeArray<LineSection> lineSections =
                sectionsQuery.ToComponentDataArrayAsync<LineSection>(Allocator.TempJob, out JobHandle jh2);

            BufferFromEntity<LineJoinPoint> lineJoinPointBuffer = GetBufferFromEntity<LineJoinPoint>();

            Dependency = JobHandle.CombineDependencies(jh, Dependency, jh2);

            Entities
                .WithReadOnly(lineSectionEntities)
                .WithDeallocateOnJobCompletion(lineSections)
                .WithDeallocateOnJobCompletion(lineSectionEntities)
                .ForEach((Entity entity, int entityInQueryIndex, ref LineToolUpdateEvent updateLineEvent) =>
                {
                    LineUpdateJob job = new LineUpdateJob
                    {
                        LineSectionEntities = lineSectionEntities,
                        LineJoinPointBuffer = lineJoinPointBuffer,
                        LineSections = lineSections,
                        Ecb = ecbConcurrent,
                        JobIndex = entityInQueryIndex
                    };

                    job.Execute(ref lineTool, ref updateLineEvent);

                    ecbConcurrent.DestroyEntity(entityInQueryIndex, entity);
                    ecbConcurrent.SetComponent(entityInQueryIndex, lineToolEntity, lineTool);
                }).Schedule(Dependency).Complete();

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        private struct LineUpdateJob
        {
            public NativeArray<Entity> LineSectionEntities;
            public NativeArray<LineSection> LineSections;
            public BufferFromEntity<LineJoinPoint> LineJoinPointBuffer;
            public EntityCommandBuffer.Concurrent Ecb;
            public int JobIndex;

            public void Execute(ref LineTool2 lineTool, ref LineToolUpdateEvent updateEvent)
            {
                if (!TryGetEndNodes(ref lineTool, out int originSectionIndex, out int endSectionIndex))
                {
                    Debug.LogWarning("Unable to get end nodes for line");
                    return;
                }

                int sectionIndex =
                    updateEvent.JoinHoldingEntity.Equals(Entity.Null)
                        ? endSectionIndex
                        : updateEvent.JoinHoldingEntity.Equals(LineSectionEntities[originSectionIndex])
                            ? originSectionIndex
                            : endSectionIndex;

                // Update the end point
                UpdateEndPoint(
                    sectionIndex,
                    updateEvent.Position,
                    updateEvent.JoinHoldingEntity.Equals(Entity.Null) ? 1 : updateEvent.JoinIndex);

                // Recalculate the bezier
                RecalculateBezier(ref lineTool, originSectionIndex, endSectionIndex);

                Ecb.AddComponent<Dirty>(JobIndex, lineTool.Data.Entity);
            }

            private void RecalculateBezier(ref LineTool2 lineTool, int originIndex, int endIndex)
            {
                int toolOriginIndex = LineSectionEntities.IndexOf<Entity>(lineTool.Data.OriginSectionEntity);
                int toolCentreIndex = LineSectionEntities.IndexOf<Entity>(lineTool.Data.CentralSectionEntity);
                int toolEndIndex = LineSectionEntities.IndexOf<Entity>(lineTool.Data.EndSectionEntity);

                var bezierData = GetBezierData(originIndex, endIndex);

                var originJoin = LineJoinPointBuffer[LineSectionEntities[originIndex]][0];
                var endJoin = LineJoinPointBuffer[LineSectionEntities[endIndex]][1];

                bool haveOriginSection, haveCentreSection, haveEndSection;
                if (!(haveOriginSection = TryCreateOrUpdateSection(ref lineTool, toolOriginIndex, bezierData.Origin,
                        ref lineTool.Data.OriginSectionEntity,
                        out DynamicBuffer<LineJoinPoint> originJoinPoints))
                    && !lineTool.Data.OriginSectionEntity.Equals(Entity.Null))
                {
                    Ecb.DestroyEntity(JobIndex, lineTool.Data.OriginSectionEntity);
                }

                if (!(haveEndSection = TryCreateOrUpdateSection(ref lineTool, toolEndIndex, bezierData.End,
                        ref lineTool.Data.EndSectionEntity,
                        out DynamicBuffer<LineJoinPoint> endJoinPoints))
                    && !lineTool.Data.EndSectionEntity.Equals(Entity.Null))
                {
                    Ecb.DestroyEntity(JobIndex, lineTool.Data.EndSectionEntity);
                }

                if (!(haveCentreSection = TryCreateOrUpdateSection(ref lineTool, toolCentreIndex, bezierData.Centre,
                        ref lineTool.Data.CentralSectionEntity,
                        out DynamicBuffer<LineJoinPoint> centreJoinPoints))
                    &&  (haveEndSection || haveOriginSection)
                    && !lineTool.Data.CentralSectionEntity.Equals(Entity.Null))
                {
                    Debug.Log("Destroying Central Entity");
                    Ecb.DestroyEntity(JobIndex, lineTool.Data.CentralSectionEntity);
                }

                if (haveOriginSection)
                {
                    UpdateJoinPoints(originJoinPoints, bezierData.Origin);

                    if (LineJoinPointBuffer.Exists(originJoin.JoinData.ConnectedEntity))
                    {
                        LineJoinPoint.Join(
                            LineJoinPointBuffer[originJoin.JoinData.ConnectedEntity],
                            originJoinPoints,
                            new LineJoinPoint.NewJoinInfo
                            {
                                FromIndex = originJoin.JoinData.ConnectedIndex,
                                FromSection = originJoin.JoinData.ConnectedEntity,
                                ToIndex = 0,
                                ToSection = lineTool.Data.OriginSectionEntity
                            });
                    }

                    if (haveCentreSection)
                    {
                        LineJoinPoint.Join(LineJoinPointBuffer, new LineJoinPoint.NewJoinInfo
                        {
                            FromIndex = 1,
                            FromSection = lineTool.Data.OriginSectionEntity,
                            ToIndex = 0,
                            ToSection = lineTool.Data.CentralSectionEntity
                        });
                    }
                    else if (haveEndSection)
                    {
                        LineJoinPoint.Join(LineJoinPointBuffer, new LineJoinPoint.NewJoinInfo
                        {
                            FromIndex = 1,
                            FromSection = lineTool.Data.OriginSectionEntity,
                            ToIndex = 0,
                            ToSection = lineTool.Data.EndSectionEntity
                        });
                    }
                    else if (LineJoinPointBuffer.Exists(endJoin.JoinData.ConnectedEntity))
                    {
                        LineJoinPoint.Join(LineJoinPointBuffer, new LineJoinPoint.NewJoinInfo
                        {
                            FromIndex = 1,
                            FromSection = lineTool.Data.OriginSectionEntity,
                            ToIndex = endJoin.JoinData.ConnectedIndex,
                            ToSection = endJoin.JoinData.ConnectedEntity
                        });
                    }
                }

                if (haveCentreSection)
                {
                    UpdateJoinPoints(centreJoinPoints, bezierData.Centre);
                    if (!haveOriginSection && LineJoinPointBuffer.Exists(originJoin.JoinData.ConnectedEntity))
                    {
                        LineJoinPoint.Join(LineJoinPointBuffer, new LineJoinPoint.NewJoinInfo
                        {
                            FromIndex = originJoin.JoinData.ConnectedIndex,
                            FromSection = originJoin.JoinData.ConnectedEntity,
                            ToIndex = 0,
                            ToSection = lineTool.Data.CentralSectionEntity
                        });
                    }

                    if (haveEndSection)
                    {
                        LineJoinPoint.Join(LineJoinPointBuffer, new LineJoinPoint.NewJoinInfo
                        {
                            FromIndex = 1,
                            FromSection = lineTool.Data.CentralSectionEntity,
                            ToIndex = 0,
                            ToSection = lineTool.Data.EndSectionEntity
                        });
                    }
                    else if (LineJoinPointBuffer.Exists(endJoin.JoinData.ConnectedEntity))
                    {
                        LineJoinPoint.Join(LineJoinPointBuffer, new LineJoinPoint.NewJoinInfo
                        {
                            FromIndex = 1,
                            FromSection = lineTool.Data.CentralSectionEntity,
                            ToIndex = endJoin.JoinData.ConnectedIndex,
                            ToSection = endJoin.JoinData.ConnectedEntity
                        });
                    }
                }

                if (haveEndSection)
                {
                    UpdateJoinPoints(endJoinPoints, bezierData.End);
                    if (!haveOriginSection && !haveCentreSection
                                           && LineJoinPointBuffer.Exists(originJoin.JoinData.ConnectedEntity))
                    {
                        LineJoinPoint.Join(LineJoinPointBuffer, new LineJoinPoint.NewJoinInfo
                        {
                            FromIndex = originJoin.JoinData.ConnectedIndex,
                            FromSection = originJoin.JoinData.ConnectedEntity,
                            ToIndex = 0,
                            ToSection = lineTool.Data.EndSectionEntity
                        });
                    }

                    if (LineJoinPointBuffer.Exists(endJoin.JoinData.ConnectedEntity))
                    {
                        LineJoinPoint.Join(LineJoinPointBuffer, new LineJoinPoint.NewJoinInfo
                        {
                            FromIndex = 1,
                            FromSection = lineTool.Data.EndSectionEntity,
                            ToIndex = endJoin.JoinData.ConnectedIndex,
                            ToSection = endJoin.JoinData.ConnectedEntity
                        });
                    }
                }
            }

            private bool TryCreateOrUpdateSection(ref LineTool2 lineTool, int index, float3x3 bezier,
                ref Entity sectionEntity,
                out DynamicBuffer<LineJoinPoint> joinPoints)
            {
                // Don't create/update zero length sections
                if (bezier.c0.IsCloseTo(bezier.c2))
                {
                    joinPoints = LineJoinPointBuffer.Exists(sectionEntity)
                        ? LineJoinPointBuffer[sectionEntity]
                        : default;
                    return false;
                }

                if (index == -1)
                {
                    sectionEntity = LineSection.NewLineSection(Ecb, JobIndex, lineTool.Data.Entity, bezier.c1,
                        out joinPoints);
                }
                else
                {
                    var section = LineSections[index];
                    section.Bezier = bezier;
                    Ecb.SetComponent(JobIndex, sectionEntity, section);
                    joinPoints = LineJoinPointBuffer[sectionEntity];
                }

                return true;
            }


            private BezierData GetBezierData(int originIndex, int endIndex)
            {
                BezierData result = default;

                float3 originPos = LineSections[originIndex].Bezier.c0;
                float3 endPos = LineSections[endIndex].Bezier.c2;

                var originLinePoint = LineJoinPointBuffer[LineSectionEntities[originIndex]][0];
                var endLinePoint = LineJoinPointBuffer[LineSectionEntities[originIndex]][1];

                bool originHasFixedDirection = originLinePoint.IsJoined
                                               && originLinePoint.Flags.HasFlags(LineJoinPoint.JoinFlags.NonFixedDirection);
                bool endHasFixedDirection = endLinePoint.IsJoined
                                            && endLinePoint.Flags != LineJoinPoint.JoinFlags.NonFixedDirection;

                // Straight line
                if (!originHasFixedDirection && !endHasFixedDirection)
                {
                    result.Origin = new float3x3(originPos, originPos, originPos);
                    result.Centre = new float3x3(originPos, math.lerp(originPos, endPos, 0.5f), endPos);
                    result.End = new float3x3(endPos, endPos, endPos);
                }

                return result;
            }

            private static void UpdateJoinPoints(DynamicBuffer<LineJoinPoint> joinPoints, float3x3 bezierData)
            {
                if (joinPoints.Length != 2)
                    return;
                joinPoints[0] = UpdateJoinPoint(joinPoints[0], bezierData.c0, bezierData.c2);
                joinPoints[1] = UpdateJoinPoint(joinPoints[1], bezierData.c2, bezierData.c0);
            }

            private static LineJoinPoint UpdateJoinPoint(LineJoinPoint point, float3 position, float3 otherPosition)
            {
                var result = point;
                result.Position = position;

                // Only update direction if not joined to another section
                if (!result.IsJoined)
                {
                    result.Direction = math.normalize(otherPosition - position);
                }

                return result;
            }

            private void UpdateEndPoint(int sectionIndex, float3 position, int join)
            {
                LineSection section = LineSections[sectionIndex];
                float3x3 bezier = section.Bezier;

                if (join == 0)
                {
                    bezier.c0 = position;
                }
                else
                {
                    bezier.c2 = position;
                }

                section.Bezier = bezier;

                LineSections[sectionIndex] = section;
            }

            private bool TryGetEndNodes(ref LineTool2 lineTool,
                out int originSectionEntity, out int endSectionEntity)
            {
                originSectionEntity = -1;
                endSectionEntity = -1;

                if (LineSectionEntities.Contains(lineTool.Data.OriginSectionEntity))
                {
                    originSectionEntity = LineSectionEntities.IndexOf<Entity>(lineTool.Data.OriginSectionEntity);
                    if (!LineSectionEntities.Contains(lineTool.Data.CentralSectionEntity))
                    {
                        if (!LineSectionEntities.Contains(lineTool.Data.EndSectionEntity))
                        {
                            endSectionEntity = originSectionEntity;

                            return true;
                        }

                        if (!LineSectionEntities.Contains(lineTool.Data.EndSectionEntity))
                        {
                            endSectionEntity = LineSectionEntities.IndexOf<Entity>(lineTool.Data.OriginSectionEntity);

                            return true;
                        }

                        return false;
                    }
                }

                if (LineSectionEntities.Contains(lineTool.Data.CentralSectionEntity))
                {
                    if (originSectionEntity.Equals(-1))
                    {
                        originSectionEntity = LineSectionEntities.IndexOf<Entity>(lineTool.Data.CentralSectionEntity);
                    }

                    if (!LineSectionEntities.Contains(lineTool.Data.EndSectionEntity))
                    {
                        endSectionEntity = originSectionEntity;
                        return true;
                    }
                }

                if (LineSectionEntities.Contains(lineTool.Data.EndSectionEntity))
                {
                    if (originSectionEntity.Equals(-1))
                    {
                        originSectionEntity = LineSectionEntities.IndexOf<Entity>(lineTool.Data.EndSectionEntity);
                    }

                    endSectionEntity = LineSectionEntities.IndexOf<Entity>(lineTool.Data.EndSectionEntity);
                    return true;
                }

                return false;
            }
        }
    }
}