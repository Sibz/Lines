using System;
using Unity.Entities;
using UnityEngine;
using Object = System.Object;

namespace Sibz.Lines.ECS.Behaviours
{
    public class EcsLineBehaviour : MonoBehaviour
    {
        public EcsLineNodeBehaviour EndNode1;
        public EcsLineNodeBehaviour EndNode2;

        public Entity LineEntity;

        //private bool destroyCorrectly;

        private void Destroy()
        {
            if (!LineWorld.World.IsCreated)
            {
                return;
            }

            if (LineWorld.Em.Exists(EndNode1.JoinPoint))
            {
                LineWorld.Em.DestroyEntity(EndNode1.JoinPoint);
            }
            if (LineWorld.Em.Exists(EndNode2.JoinPoint))
            {
                LineWorld.Em.DestroyEntity(EndNode2.JoinPoint);
            }
            if (LineWorld.Em.Exists(LineEntity))
            {
                LineWorld.Em.DestroyEntity(LineEntity);
            }
            //destroyCorrectly = true;
            //Destroy(gameObject);
        }

        public void Complete()
        {
            EndNode1.GetComponent<Collider>().enabled = true;
            EndNode2.GetComponent<Collider>().enabled = true;
        }

        private void OnDestroy()
        {
            Destroy();
            /*if (LineWorld.World.IsCreated && LineWorld.Em.Exists(LineEntity) && !destroyCorrectly)
            {
                Debug.LogWarning(
                    "Line was not destroyed using EcsLineBehaviour.Destroy method. "+
                    "This will not clean up relevant entities");
            }*/
        }
    }
}