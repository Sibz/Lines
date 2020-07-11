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
                        
                    }
                    EntityManager.DestroyEntity(eventEntity);
                }).WithoutBurst().Run();
        }
    }
}