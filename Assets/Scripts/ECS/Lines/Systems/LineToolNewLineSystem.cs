using System;
using Sibz.Lines.ECS.Behaviours;
using Sibz.Lines.ECS.Components;
using Sibz.Lines.ECS.Enums;
using Sibz.Lines.ECS.Events;
using Sibz.Lines.ECS.Jobs;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Sibz.Lines.ECS.Systems
{
    public class LineToolNewLineSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireSingletonForUpdate<LineTool>();
        }

        protected override void OnUpdate()
        {
            LineTool lineTool = GetSingleton<LineTool>();

            if (lineTool.State != LineToolState.Idle)
            {
                return;
            }

            Entities
                .WithStructuralChanges()
                .ForEach((Entity eventEntity, ref NewLineEvent newLineEvent) =>
                {
                    new LineToolCreateLineJob
                    {
                        EntityManager = EntityManager,
                        NewLineEvent = newLineEvent
                    }.Execute(ref lineTool);
                    EntityManager.DestroyEntity(eventEntity);
                }).WithoutBurst().Run();

            LineEndSimBufferSystem.Instance.AddJobHandleForProducer(Dependency);
            SetSingleton(lineTool);
        }
    }
}