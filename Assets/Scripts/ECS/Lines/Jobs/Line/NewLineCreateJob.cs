using Sibz.Lines.ECS.Behaviours;
using Sibz.Lines.ECS.Components;
using Sibz.Lines.ECS.Enums;
using Sibz.Lines.ECS.Events;
using Unity.Entities;
using Unity.Mathematics;

namespace Sibz.Lines.ECS.Jobs
{
    public struct NewLineCreateJob
    {
        public EntityManager      EntityManager;
        public NewLineCreateEvent NewLineCreateEvent;

        private LineTool lineTool;

        public void Execute(ref LineTool tool)
        {
            lineTool = tool;

            // TODO: Load profile for line
            lineTool.Data.LineEntity = Line.New(NewLineCreateEvent.StartingPosition, Line.Prefab);

            EntityManager.SetComponentData(lineTool.Data.LineEntity,
                                           new NewLine
                                           {
                                               Modifiers = NewLineModifiers.Defaults()
                                           });

            CreateLineJoinPoints(lineTool.Data);

            lineTool.State = LineToolState.Editing;

            tool = lineTool;
        }

        private void CreateLineJoinPoints(LineToolData data)
        {
            var lineObject    = EntityManager.GetComponentObject<EcsLineBehaviour>(data.LineEntity);
            var lineComponent = EntityManager.GetComponentData<Line>(data.LineEntity);
            lineObject.LineEntity = data.LineEntity;

            var direction = float3.zero;
            if (EntityManager.Exists(NewLineCreateEvent.FromJoinPointEntity))
                direction = -EntityManager.GetComponentData<LineJoinPoint>(NewLineCreateEvent.FromJoinPointEntity)
                                          .Direction;

            lineComponent.JoinPointA = lineObject.EndNode1.JoinPoint =
                                           LineJoinPoint.New(data.LineEntity, NewLineCreateEvent.StartingPosition,
                                                             direction);
            lineComponent.JoinPointB = lineObject.EndNode2.JoinPoint =
                                           LineJoinPoint.New(data.LineEntity, NewLineCreateEvent.StartingPosition,
                                                             direction);

            EntityManager.SetComponentData(data.LineEntity, lineComponent);

            if (EntityManager.Exists(NewLineCreateEvent.FromJoinPointEntity))
                LineJoinPoint.Join(NewLineCreateEvent.FromJoinPointEntity, lineObject.EndNode1.JoinPoint);
        }
    }
}