using Sibz.Lines.ECS.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Sibz.Lines.ECS.Jobs
{
    public struct LineToolUpdateGatherJoinsJob : IJob
    {
        public EntityManager             Em;
        public Entity                    LineEntity;
        public Entity                    JoinToEntity;
        public NativeList<Entity>        JoinEntities;
        public NativeList<LineJoinPoint> JoinPoints;

        public void Execute()
        {
            var line = Em.GetComponentData<Line>(LineEntity);

            AddJoinPoint(line.JoinPointA);
            AddJoinPoint(line.JoinPointB);

            if (!Em.Exists(JoinToEntity)) return;

            JoinEntities.Add(JoinToEntity);
            JoinPoints.Add(Em.GetComponentData<LineJoinPoint>(JoinToEntity));
        }

        public void AddJoinPoint(Entity joinPointEntity)
        {
            var jp = Em.GetComponentData<LineJoinPoint>(joinPointEntity);
            JoinEntities.Add(joinPointEntity);
            JoinPoints.Add(jp);
            if (!Em.Exists(jp.JoinToPointEntity)) return;

            JoinEntities.Add(jp.JoinToPointEntity);
            JoinPoints.Add(Em.GetComponentData<LineJoinPoint>(jp.JoinToPointEntity));
        }
    }
}