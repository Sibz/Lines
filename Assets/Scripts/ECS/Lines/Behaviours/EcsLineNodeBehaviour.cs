using System.Collections;
using Sibz.Lines.ECS.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Sibz.Lines.ECS.Behaviours
{
    public class EcsLineNodeBehaviour : MonoBehaviour
    {
        public  Entity        JoinPoint;
        private Collider      col;
        public  LineJoinPoint JoinPointData => LineWorld.Em.GetComponentData<LineJoinPoint>(JoinPoint);

        private void OnEnable()
        {
            col = GetComponent<Collider>();
        }

        public void UpdateFromEntity()
        {
            var data = JoinPointData;

            var tx = transform;

            if (!data.Direction.Equals(float3.zero) && !float.IsNaN(data.Direction.x))
            {
                tx.rotation = Quaternion.LookRotation(data.Direction);
                tx.position =
                    data.Pivot + data.Direction * data.DistanceFromPivot;
            }
            else
            {
                tx.position = data.Pivot;
            }
        }

        public void OnComplete()
        {
            col.enabled = true;

            if (LineWorld.Em.Exists(JoinPointData.JoinToPointEntity))
            {
                if (!gameObject.activeSelf) return;

                StartCoroutine(SetInActiveEndOfFrame());
                return;
            }

            gameObject.SetActive(true);
        }

        private IEnumerator SetInActiveEndOfFrame()
        {
            yield return new WaitForEndOfFrame();
            var otherJoinData = LineWorld.Em.GetComponentData<LineJoinPoint>(JoinPointData.JoinToPointEntity);
            gameObject.SetActive(false);

            LineWorld.Em.GetComponentObject<EcsLineBehaviour>(otherJoinData.ParentEntity)?.OnComplete();
        }
    }
}