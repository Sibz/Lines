using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Sibz.Lines.ECS.Components
{
    public struct Line : IComponentData
    {
        
        public static void New(float3 position, GameObject prefab)
        {
            var go = Object.Instantiate(prefab);

        }
    }
}