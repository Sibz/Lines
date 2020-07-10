using Unity.Collections;
using Unity.Entities;
// ReSharper disable AccessToDisposedClosure

namespace Sibz.Lines.Systems
{
    [UpdateInGroup(typeof(LineSystemGroup), OrderLast = true)]
    public class LineCreateSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var lineTool = GetSingleton<LineTool2>();
            var lineToolEntity = GetSingletonEntity<LineTool2>();
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var ecbConcurrent = ecb.ToConcurrent();
            var archetype = Line2.LineArchetype;

            Entities.ForEach((Entity entity, int entityInQueryIndex, ref LineToolCreateLineEvent lineEvent) =>
            {

                var data = lineTool.Data;
                if (lineTool.State == LineTool2.ToolState.Idle)
                {
                    data.Entity = Line2.New(ecbConcurrent,entityInQueryIndex, lineEvent.Position, archetype, out Entity initialSection);

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