using Unity.Entities;
using UnityEngine;

namespace Sibz.Lines.ECS.Behaviours
{
    public class EcsLineBehaviour : MonoBehaviour
    {
        public EcsLineNodeBehaviour EndNode1;
        public EcsLineNodeBehaviour EndNode2;

        public Entity LineEntity;

        public void OnDirty()
        {
            EndNode1.UpdateFromEntity();
            EndNode2.UpdateFromEntity();
        }
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
        }

        public void OnComplete()
        {
            EndNode1.OnComplete();
            EndNode2.OnComplete();
        }

        private void OnDestroy()
        {
            Destroy();
        }
    }
}