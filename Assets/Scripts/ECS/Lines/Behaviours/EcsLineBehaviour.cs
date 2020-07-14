using Unity.Entities;
using UnityEngine;

namespace Sibz.Lines.ECS.Behaviours
{
    [RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
    public class EcsLineBehaviour : MonoBehaviour
    {
        public EcsLineNodeBehaviour EndNode1;
        public EcsLineNodeBehaviour EndNode2;

        public Entity LineEntity;

        public MeshRenderer MeshRenderer { get; private set; }
        public MeshFilter   MeshFilter   { get; private set; }

        private void OnEnable()
        {
            MeshRenderer = GetComponent<MeshRenderer>();
            MeshFilter   = GetComponent<MeshFilter>();
        }

        public void OnDirty()
        {
            EndNode1.UpdateFromEntity();
            EndNode2.UpdateFromEntity();
        }

        private void Destroy()
        {
            if (!LineWorld.World.IsCreated) return;

            // Use a destroy event so line and associated entities are removed
            // and any joins can be unjoint
            if (LineWorld.Em.Exists(EndNode1.JoinPoint)) LineWorld.Em.DestroyEntity(EndNode1.JoinPoint);

            if (LineWorld.Em.Exists(EndNode2.JoinPoint)) LineWorld.Em.DestroyEntity(EndNode2.JoinPoint);

            if (LineWorld.Em.Exists(LineEntity)) LineWorld.Em.DestroyEntity(LineEntity);
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