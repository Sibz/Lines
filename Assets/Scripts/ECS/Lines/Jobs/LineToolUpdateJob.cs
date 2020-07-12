using Sibz.Lines.ECS.Components;
using Sibz.Lines.ECS.Events;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Sibz.Lines.ECS.Jobs
{
    public struct LineToolUpdateJob
    {
        public NewLineUpdateEvent EventData;
        public EntityCommandBuffer.Concurrent Ecb;
        public NativeArray<Entity> JoinPointEntities;
        public NativeArray<LineJoinPoint> JoinPoints;
        public int JobIndex;

        private LineTool lineTool;
        public void Execute(ref LineTool lineToolIn)
        {
            lineTool = lineToolIn;

            UpdateJoinPoint();

            SetLineDirty();

            lineToolIn = lineTool;
        }

        private void SetLineDirty()
        {
            Ecb.AddComponent<Dirty>(JobIndex, lineTool.Data.LineEntity);
        }

        private void UpdateJoinPoint()
        {
            int joinIndex = JoinPointEntities.IndexOf<Entity>(EventData.JoinPoint);
            if (joinIndex == -1)
            {
                Debug.LogWarning("Tried to update join point that doesn't exist or isn't editable");
                return;
            }

            var joinPointEntity = JoinPointEntities[joinIndex];
            var joinPoint = JoinPoints[joinIndex];
            joinPoint.Pivot = EventData.Position;

            if (!EventData.JoinTo.Equals(Entity.Null))
            {
                LineJoinPoint.Join(Ecb, JobIndex,
                    EventData.JoinToData, EventData.JoinTo,
                    joinPoint, joinPointEntity);
            }
            else
            {
                Ecb.SetComponent(JobIndex, joinPointEntity, joinPoint);
            }
        }
    }
}