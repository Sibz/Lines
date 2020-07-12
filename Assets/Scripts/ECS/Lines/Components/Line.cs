using Sibz.Lines.ECS.Behaviours;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Sibz.Lines.ECS.Components
{
    public struct Line : IComponentData
    {
        public static EntityArchetype LineArchetype = LineWorld.Em
            .CreateArchetype(typeof(Line), typeof(LineKnotData), typeof(NewLine));

        public float3 Position;
        public Entity JoinPointA;
        public Entity JoinPointB;

        private static GameObject prefab;
        public static GameObject Prefab => prefab == null
            ? (prefab = Resources.Load<GameObject>("prefabs/ecsLine"))
            : prefab;

        public static Entity New(float3 position, GameObject prefab)
        {
            Entity result = LineWorld.Em.CreateEntity(LineArchetype);
            LineWorld.Em.SetComponentData(result, new Line{ Position = position});
            LineWorld.Em.AddComponentObject(result,
                Object.Instantiate(prefab, position, Quaternion.identity).GetComponent<EcsLineBehaviour>());
            return result;
        }

    }
}