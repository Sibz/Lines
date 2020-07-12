using Unity.Entities;
using UnityEngine;

namespace Sibz.Lines.ECS.Behaviours
{
    public class EcsLineBehaviour : MonoBehaviour
    {
        public EcsLineNodeBehaviour EndNode1;
        public EcsLineNodeBehaviour EndNode2;

        public Entity EcsLineEntity;
    }
}