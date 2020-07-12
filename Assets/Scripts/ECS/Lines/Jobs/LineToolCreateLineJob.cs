using Sibz.Lines.ECS.Behaviours;
using Sibz.Lines.ECS.Components;
using Sibz.Lines.ECS.Enums;
using Sibz.Lines.ECS.Events;
using Unity.Entities;

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
            lineTool.Data.Modifiers.From.Position = NewLineEvent.StartingPosition;
            lineTool.Data.Modifiers.From.JoinPoint = NewLineEvent.FromJoinPointEntity;
            lineTool.Data.Modifiers.To.Position = NewLineEvent.StartingPosition;

            CreateLineJoinPoints(lineTool.Data);

            lineTool.State = LineToolState.Editing;

            tool = lineTool;
        }
        private void CreateLineJoinPoints(LineToolData data)
        {
            EcsLineBehaviour lineObject = EntityManager.GetComponentObject<EcsLineBehaviour>(data.LineEntity);
            lineObject.LineEntity = data.LineEntity;

            lineObject.EndNode1.JoinPoint = LineJoinPoint.New(data.LineEntity, data.Modifiers.From.Position);
            lineObject.EndNode2.JoinPoint = LineJoinPoint.New(data.LineEntity, data.Modifiers.To.Position);

            if (EntityManager.Exists(data.Modifiers.From.JoinPoint))
            {
                LineJoinPoint.Join(data.Modifiers.From.JoinPoint, lineObject.EndNode1.JoinPoint);
            }
        }
    }
}