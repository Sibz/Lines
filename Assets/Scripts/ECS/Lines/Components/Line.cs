using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Sibz.Lines.ECS.Components
{
    public struct Line : IComponentData
    {
        public static EntityArchetype LineArchetype = LineWorld.Em
            .CreateArchetype(typeof(Line), typeof(LineJoinPoint), typeof(NewLine));

        public static Entity New(float3 position, GameObject prefab)
        {
            Entity result = LineWorld.Em.CreateEntity(LineArchetype);
            LineWorld.Em.AddComponentObject(result,
                Object.Instantiate(prefab, position, Quaternion.identity));
            return result;
        }

    }
}