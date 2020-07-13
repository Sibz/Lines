using Sibz.Lines.ECS.Components;
using Sibz.Lines.ECS.Enums;
using Sibz.Lines.ECS.Events;
using Unity.Entities;

namespace Sibz.Lines.ECS.Systems
{
    public class LineToolCompleteSystem : SystemBase
    {
        private EntityQuery query;
        protected override void OnCreate()
        {
            query = GetEntityQuery(typeof(NewLineCompleteEvent));
            RequireSingletonForUpdate<LineTool>();
            RequireForUpdate(query);
        }

        protected override void OnUpdate()
        {
            LineTool lineTool = GetSingleton<LineTool>();

            if (lineTool.State != LineToolState.Editing)
            {
                return;
            }

            Entities
                .WithStructuralChanges()
                .WithAll<NewLineCompleteEvent>()
                .ForEach((Entity eventEntity) =>
                {
                    if (lineTool.LineBehaviour)
                    {
                        lineTool.LineBehaviour.OnComplete();
                    }

                    var line = EntityManager.GetComponentData<Line>(lineTool.Data.LineEntity);
                    EntityManager.RemoveComponent<NewLine>(lineTool.Data.LineEntity);
                    EntityManager.RemoveComponent<JoinEditable>(line.JoinPointA);
                    EntityManager.RemoveComponent<JoinEditable>(line.JoinPointB);

                    lineTool = LineTool.Default();
                    SetSingleton(lineTool);


                }).WithoutBurst().Run();

            EntityManager.DestroyEntity(query);
        }

    }
}