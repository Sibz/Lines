using Unity.Entities;

namespace Sibz.Lines.Tools.Systems
{
    public class LineToolFinishSystem : SystemBase
    {
        private EntityQuery query;
        protected override void OnUpdate()
        {
            var lineTool = GetSingleton<LineTool2>();
            Entities
                .WithStoreEntityQueryInField(ref query)
                .WithAll<LineToolFinishEvent>()
                .WithStructuralChanges()
                .ForEach((Entity entity) =>
                {
                    EntityManager.AddComponent<Dirty>(lineTool.Data.Entity);
                    lineTool.Data = default;
                    lineTool.State = LineTool2.ToolState.Idle;
                    SetSingleton(lineTool);
                }).WithoutBurst().Run();

            EntityManager.DestroyEntity(query);
        }
    }
}