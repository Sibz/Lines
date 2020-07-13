using Sibz.Lines.ECS.Components;
using Sibz.Lines.ECS.Events;
using Unity.Entities;
using Unity.Mathematics;

namespace Sibz.Lines.ECS.Systems
{
    [UpdateInGroup(typeof(LineWorldInitGroup))]
    public class LineToolUpdateModSystem : SystemBase
    {

        private EntityQuery changeModEventQuery;
        protected override void OnCreate()
        {
            changeModEventQuery = GetEntityQuery(typeof(LineToolModChangeEvent));
        }

        protected override void OnUpdate()
        {
            LineTool lineTool = GetSingleton<LineTool>();
            Entities
                .WithStructuralChanges()
                .ForEach((Entity entity, int entityInQueryIndex, ref LineToolModChangeEvent evt) =>
                {
                    static void Mod(ref LineToolData.ToolModifiers.EndMods  l,
                        LineToolData.ToolModifiers.EndMods r)
                    {
                        // TODO: Settings should be imported either from tool settings or line profile
                        l.Size = math.max(0.25f, l.Size+ r.Size);
                        l.Ratio = math.clamp(l.Ratio + r.Ratio, 0.5f, 1.5f);
                        l.Height += r.Height;
                        l.InnerHeight += r.InnerHeight;
                        l.InnerHeightDistanceFromEnd += l.InnerHeightDistanceFromEnd;
                    }

                    Mod(ref lineTool.Data.Modifiers.From, evt.ModifierChangeValues.From);
                    Mod(ref lineTool.Data.Modifiers.To, evt.ModifierChangeValues.To);

                    SetSingleton(lineTool);
                    Line line = EntityManager.GetComponentData<Line>(lineTool.Data.LineEntity);
                    LineJoinPoint joinPoint = EntityManager.GetComponentData<LineJoinPoint>(line.JoinPointB);
                    NewLineUpdateEvent.New(line.JoinPointB, joinPoint.Pivot, joinPoint.JoinToPointEntity);

                }).WithoutBurst().Run();

            EntityManager.DestroyEntity(changeModEventQuery);


        }
    }
}