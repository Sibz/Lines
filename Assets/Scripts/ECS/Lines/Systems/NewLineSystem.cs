using Sibz.Lines.ECS.Components;
using Sibz.Lines.ECS.Enums;
using Sibz.Lines.ECS.Events;
using Unity.Entities;
using UnityEngine;

namespace Sibz.Lines.ECS.Systems
{
    public class NewLineSystem : SystemBase
    {
        private GameObject prefab;

        protected override void OnCreate()
        {
            prefab = Resources.Load<GameObject>("prefabs/ecsLine");
        }

        protected override void OnUpdate()
        {
            var lineTool = GetSingleton<LineTool>();

            Entities
                .WithStructuralChanges()
                .ForEach((Entity eventEntity, ref NewLineEvent newLineEvent) =>
                {
                    if (lineTool.State == LineToolState.Idle)
                    {
                        lineTool.Data.LineEntity = Line.New(newLineEvent.StartingPosition, prefab);
                        lineTool.Data.Modifiers.From.Position = newLineEvent.StartingPosition;
                        lineTool.Data.Modifiers.From.JoinPoint = newLineEvent.FromJoinPointEntity;
                        lineTool.State = LineToolState.New;
                        SetSingleton(lineTool);
                    }
                    EntityManager.DestroyEntity(eventEntity);
                }).WithoutBurst().Run();
        }
    }
}