using Sibz.Lines.ECS.Behaviours;
using Sibz.Lines.ECS.Components;
using Sibz.Lines.ECS.Enums;
using Sibz.Lines.ECS.Events;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Sibz.Lines.ECS.Jobs
{
    public struct LineToolCreateLineJob
    {
        public EntityManager EntityManager;
        public NewLineEvent NewLineEvent;

        private LineTool lineTool;

        public void Execute(ref LineTool tool)
        {
            lineTool = tool;

            // TODO: Load profile for line
            lineTool.Data.LineEntity = Line.New(NewLineEvent.StartingPosition, Line.Prefab);

            CreateLineJoinPoints(lineTool.Data);

            lineTool.State = LineToolState.Editing;

            tool = lineTool;
        }
        private void CreateLineJoinPoints(LineToolData data)
        {
            EcsLineBehaviour lineObject = EntityManager.GetComponentObject<EcsLineBehaviour>(data.LineEntity);
            Line lineComponent = EntityManager.GetComponentData<Line>(data.LineEntity);
            lineObject.LineEntity = data.LineEntity;

            float3 direction = float3.zero;
            if (EntityManager.Exists(NewLineEvent.FromJoinPointEntity))
            {
                direction = -EntityManager.GetComponentData<LineJoinPoint>(NewLineEvent.FromJoinPointEntity).Direction;
            }
            lineComponent.JoinPointA = lineObject.EndNode1.JoinPoint =
                LineJoinPoint.New(data.LineEntity, NewLineEvent.StartingPosition, direction);
            lineComponent.JoinPointB = lineObject.EndNode2.JoinPoint =
                LineJoinPoint.New(data.LineEntity, NewLineEvent.StartingPosition, direction);

            EntityManager.SetComponentData(data.LineEntity, lineComponent);

            if (EntityManager.Exists(NewLineEvent.FromJoinPointEntity))
            {
                LineJoinPoint.Join(NewLineEvent.FromJoinPointEntity, lineObject.EndNode1.JoinPoint);
            }
        }
    }
}