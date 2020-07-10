using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Sibz.Lines.Systems
{
    public class LineToolSystem : SystemBase
    {
        /*
        protected override void OnUpdate()
        {
            LineTool2 lineTool = GetSingleton<LineTool2>();
            Entity lineToolEntity = GetSingletonEntity<LineTool2>();

            if (lineTool.State == LineTool2.ToolState.Idle
                && EntityManager.HasComponent<LineStart>(lineToolEntity))
            {
             //   LineStart(ref lineTool, lineToolEntity);
            }
            else if (lineTool.State == LineTool2.ToolState.EditLine
                     && EntityManager.HasComponent<LineUpdate>(lineToolEntity))
            {
                LineUpdate(ref lineTool, lineToolEntity);
            }

            EntityManager.SetComponentData(lineToolEntity, lineTool);
        }

        private void LineUpdate(ref LineTool2 lineTool, Entity lineToolEntity)
        {
            LineUpdate lineUpdate = GetComponent<LineUpdate>(lineToolEntity);
            if (!TryGetEndNodes(ref lineTool, out Entity originSectionEntity, out Entity endSectionEntity))
            {
                Debug.LogWarning("Unable to get end nodes for line");
                return;
            }

            Entity updateSectionEntity =
                lineUpdate.Section.Equals(originSectionEntity)
                    ? originSectionEntity
                    : endSectionEntity;

            // Update the end point
            UpdateEndPoint(
                updateSectionEntity,
                lineUpdate.Position, lineUpdate.Join);

            // Recalculate the bezier
            RecalculateBezier(ref lineTool, originSectionEntity, endSectionEntity);
        }

        private void RecalculateBezier(ref LineTool2 lineTool, Entity origin, Entity end)
        {
            var buff = GetBufferFromEntity<LineJoinPoint>();

            float3 originPos = GetComponent<LineSection>(origin).Bezier.c0;
            float3 endPos = GetComponent<LineSection>(end).Bezier.c2;

            bool originIsJoined = !buff[origin][0].ConnectedEntity.Equals(Entity.Null);
            bool endIsJoined = !buff[end][1].ConnectedEntity.Equals(Entity.Null);

            Entity newOriginSection = Entity.Null;
            Entity newCentreSection = Entity.Null;
            Entity newEndSection = Entity.Null;
            // Straight line
            if (!originIsJoined && !endIsJoined)
            {
                var bezierData = new BezierData
                {
                    Origin = float3x3.zero,
                    Centre = new float3x3(originPos, math.lerp(originPos, endPos, 0.5f), endPos),
                    End = float3x3.zero
                };
                newCentreSection =
                    LineSection.NewLineSection(EntityManager, lineTool.Data.Entity, bezierData.Centre.c1);
                var joinPoints = EntityManager.GetBuffer<LineJoinPoint>(newCentreSection);
                var point1 = joinPoints[0];
                var point2 = joinPoints[1];
                point1.Position = bezierData.Centre.c0;
                point2.Position = bezierData.Centre.c2;
                point1.Direction = math.normalize(point2.Position - point1.Position);
                point2.Direction = math.normalize(point1.Position - point2.Position);
                joinPoints[0] = point1;
                joinPoints[1] = point2;
            }

            /*LineSectionReplaceEvent.New(EntityManager, lineTool.Data.Entity,
                out DynamicBuffer<LineSectionReplaceEventData> buffer);
            buffer.Add(new LineSectionReplaceEventData
                { NewEntity = newOriginSection, OldEntity = lineTool.Data.OriginSectionEntity });
            buffer.Add(new LineSectionReplaceEventData
                { NewEntity = newCentreSection, OldEntity = lineTool.Data.CentralSectionEntity });
            buffer.Add(new LineSectionReplaceEventData
                { NewEntity = newEndSection, OldEntity = lineTool.Data.EndSectionEntity });#1#
            if (lineTool.Data.OriginSectionEntity != Entity.Null)
            {
                EntityManager.DestroyEntity(lineTool.Data.OriginSectionEntity);
            }
            if (lineTool.Data.CentralSectionEntity != Entity.Null)
            {
                EntityManager.DestroyEntity(lineTool.Data.CentralSectionEntity);
            }
            if (lineTool.Data.EndSectionEntity != Entity.Null)
            {
                EntityManager.DestroyEntity(lineTool.Data.EndSectionEntity);
            }

            lineTool.Data.OriginSectionEntity = newOriginSection;
            lineTool.Data.CentralSectionEntity = newCentreSection;
            lineTool.Data.EndSectionEntity = newEndSection;
        }

        private void UpdateEndPoint(Entity entity, float3 position, int join)
        {
            LineSection section = GetComponent<LineSection>(entity);
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
            SetComponent(entity, section);
        }

        private bool TryGetEndNodes(ref LineTool2 lineTool,
            out Entity originSectionEntity, out Entity endSectionEntity)
        {
            originSectionEntity = Entity.Null;
            endSectionEntity = Entity.Null;

            if (EntityManager.Exists(lineTool.Data.OriginSectionEntity))
            {
                originSectionEntity = lineTool.Data.OriginSectionEntity;
                if (!EntityManager.Exists(lineTool.Data.CentralSectionEntity))
                {
                    if (!EntityManager.Exists(lineTool.Data.EndSectionEntity))
                    {
                        endSectionEntity = originSectionEntity;

                        return true;
                    }

                    if (!EntityManager.Exists(lineTool.Data.EndSectionEntity))
                    {
                        endSectionEntity = lineTool.Data.EndSectionEntity;

                        return true;
                    }

                    return false;
                }
            }

            if (EntityManager.Exists(lineTool.Data.CentralSectionEntity))
            {
                if (!originSectionEntity.Equals(Entity.Null))
                {
                    originSectionEntity = lineTool.Data.CentralSectionEntity;
                }

                if (!EntityManager.Exists(lineTool.Data.EndSectionEntity))
                {
                    endSectionEntity = originSectionEntity;
                    return true;
                }
            }

            if (EntityManager.Exists(lineTool.Data.EndSectionEntity))
            {
                if (!originSectionEntity.Equals(Entity.Null))
                {
                    originSectionEntity = lineTool.Data.EndSectionEntity;
                }

                endSectionEntity = lineTool.Data.EndSectionEntity;
                return true;
            }

            return false;
        }*/

        /*private void LineStart(ref LineTool2 lineTool, Entity lineToolEntity)
        {
            LineStart lineStart = GetComponent<LineStart>(lineToolEntity);

            lineTool.Data.Entity = Line2.NewLine(EntityManager, lineStart.Position, out Entity initialSection);

            lineTool.Data.CentralSectionEntity = initialSection;

            lineTool.State = LineTool2.ToolState.EditLine;
        }*/
        protected override void OnUpdate()
        {

        }
    }
}