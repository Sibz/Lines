using System.Collections;
using Sibz.Lines.ECS.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Sibz.Lines.ECS.Behaviours
{
    public class EcsLineNodeBehaviour : MonoBehaviour
    {
        [SerializeField]
        public Entity JoinPoint;

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

            if (data.IsJoined && gameObject.activeSelf)
                gameObject.SetActive(false);

            else if (!data.IsJoined && !gameObject.activeSelf)
                gameObject.SetActive(true);
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
            if (LineWorld.Em.Exists(JoinPointData.JoinToPointEntity))
            {
                var otherJoinData = LineWorld.Em.GetComponentData<LineJoinPoint>(JoinPointData.JoinToPointEntity);
                LineWorld.Em.GetComponentObject<EcsLineBehaviour>(otherJoinData.ParentEntity)?.OnComplete();
            }

            gameObject.SetActive(false);
        }
    }
}