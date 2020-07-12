using Sibz.Lines.ECS.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Sibz.Lines.ECS.Behaviours
{
    public class EcsLineNodeBehaviour : MonoBehaviour
    {
        public Entity JoinPoint;

        public LineJoinPoint JoinPointData => LineWorld.Em.GetComponentData<LineJoinPoint>(JoinPoint);

        public void UpdateFromEntity()
        {
            var data = JoinPointData;
            var tx = transform;

            if (!data.Direction.Equals(float3.zero) && !float.IsNaN(data.Direction.x))
            {
                tx.rotation = Quaternion.LookRotation(data.Direction);
                tx.position =
                    data.Pivot  + (data.Direction * data.DistanceFromPivot);
            }
            else
            {
                tx.position = data.Pivot;
            }
        }
    }
}