﻿using Unity.Collections;
using Unity.Entities;
// ReSharper disable AccessToDisposedClosure

namespace Sibz.Lines.Systems
{
    [UpdateInGroup(typeof(LineSystemGroup), OrderLast = true)]
    public class LineCreateSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            LineTool2 lineTool = GetSingleton<LineTool2>();
            Entity lineToolEntity = GetSingletonEntity<LineTool2>();
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
            EntityCommandBuffer.Concurrent ecbConcurrent = ecb.ToConcurrent();
            EntityArchetype archetype = Line2.LineArchetype;
            var buffer = GetBufferFromEntity<LineJoinPoint>();

            Entities.ForEach((Entity entity, int entityInQueryIndex, ref LineToolCreateLineEvent lineEvent) =>
            {

                var data = lineTool.Data;
                if (lineTool.State == LineTool2.ToolState.Idle)
                {
                    data.Entity = Line2.New(
                        ecbConcurrent,entityInQueryIndex, lineEvent.Position, archetype, out Entity initialSection,
                        out DynamicBuffer<LineJoinPoint> joinBuffer);

                    if (!lineEvent.JoinTo.ConnectedEntity.Equals(Entity.Null))
                    {
                        LineJoinPoint.Join(
                            buffer[lineEvent.JoinTo.ConnectedEntity],
                            joinBuffer,
                            new LineJoinPoint.NewJoinInfo
                            {
                                FromIndex = lineEvent.JoinTo.ConnectedIndex,
                                FromSection = lineEvent.JoinTo.ConnectedEntity,
                                ToIndex = 0,
                                ToSection = initialSection
                            });
                    }

                    data.CentralSectionEntity = initialSection;

                    lineTool.State = LineTool2.ToolState.EditLine;
                }
                ecbConcurrent.DestroyEntity(entityInQueryIndex, entity);
                lineTool.Data = data;
                ecbConcurrent.SetComponent(entityInQueryIndex, lineToolEntity, lineTool);
            }).Schedule(Dependency).Complete();

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}