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

            NativeArray<Entity> lineSectionEntities = sectionsQuery.ToEntityArrayAsync(Allocator.TempJob, out JobHandle jh);
            NativeArray<LineSection> lineSections =
                sectionsQuery.ToComponentDataArrayAsync<LineSection>(Allocator.TempJob, out JobHandle jh2);

            BufferFromEntity<LineJoinPoint> lineJoinPointBuffer = GetBufferFromEntity<LineJoinPoint>();

            Dependency = JobHandle.CombineDependencies(jh, Dependency, jh2);

            Entities
                .WithReadOnly(lineSectionEntities)
                .WithDeallocateOnJobCompletion(lineSections)
                .WithDeallocateOnJobCompletion(lineSectionEntities)
                .ForEach((Entity entity, int entityInQueryIndex, ref LineToolUpdateLineEvent updateLineEvent) =>
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

            public void Execute(ref LineTool2 lineTool, ref LineToolUpdateLineEvent updateLineEvent)
            {
                if (!TryGetEndNodes(ref lineTool, out int originSectionIndex, out int endSectionIndex))
                {
                    Debug.LogWarning("Unable to get end nodes for line");
                    return;
                }

                int sectionIndex =
                    updateLineEvent.Section.Equals(Entity.Null)
                        ? endSectionIndex
                        : updateLineEvent.Section.Equals(LineSectionEntities[originSectionIndex])
                            ? originSectionIndex
                            : endSectionIndex;

                // Update the end point
                UpdateEndPoint(
                    sectionIndex,
                    updateLineEvent.Position,
                    updateLineEvent.Section.Equals(Entity.Null) ? 1 : updateLineEvent.JoinIndex);

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

                CreateOrUpdateSection(ref lineTool, toolCentreIndex, bezierData.Centre,
                    out DynamicBuffer<LineJoinPoint> centreJoinPoints);

                UpdateJoinPoints(centreJoinPoints, bezierData.Centre);
            }

            private void CreateOrUpdateSection(ref LineTool2 lineTool, int index, float3x3 bezier,
                out DynamicBuffer<LineJoinPoint> joinPoints)
            {
                if (index == -1)
                {
                    lineTool.Data.CentralSectionEntity =
                        LineSection.NewLineSection(Ecb, JobIndex, lineTool.Data.Entity, bezier.c1,
                            out joinPoints);
                }
                else
                {
                    var section = LineSections[index];
                    section.Bezier = bezier;
                    Ecb.SetComponent(JobIndex, lineTool.Data.CentralSectionEntity, section);
                    joinPoints = LineJoinPointBuffer[lineTool.Data.CentralSectionEntity];
                }
            }


            private BezierData GetBezierData(int originIndex, int endIndex)
            {
                BezierData result = default;

                float3 originPos = LineSections[originIndex].Bezier.c0;
                float3 endPos = LineSections[endIndex].Bezier.c2;

                bool originIsJoined = LineJoinPointBuffer[LineSectionEntities[originIndex]][0]
                    .IsJoined;
                bool endIsJoined = LineJoinPointBuffer[LineSectionEntities[endIndex]][1]
                    .IsJoined;

                // Straight line
                if (!originIsJoined && !endIsJoined)
                {
                    result.Centre = new float3x3(originPos, math.lerp(originPos, endPos, 0.5f), endPos);
                }

                return result;
            }

            private static void UpdateJoinPoints(DynamicBuffer<LineJoinPoint> joinPoints, float3x3 bezierData)
            {
                var point1 = joinPoints[0];
                var point2 = joinPoints[1];
                point1.Position = bezierData.c0;
                point2.Position = bezierData.c2;
                point1.Direction = math.normalize(point2.Position - point1.Position);
                point2.Direction = math.normalize(point1.Position - point2.Position);
                joinPoints[0] = point1;
                joinPoints[1] = point2;
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