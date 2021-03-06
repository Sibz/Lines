﻿using Sibz.Lines.ECS.Behaviours;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Sibz.Lines.ECS.Components
{
    public struct Line : IComponentData
    {
        public static EntityArchetype LineArchetype =
            LineWorld.Em.CreateArchetype(
                                         typeof(Line),
                                         typeof(LineKnotData),
                                         typeof(MeshTriangleData),
                                         typeof(MeshVertexData),
                                         typeof(NewLine));

        public float3 Position;
        public Entity JoinPointA;
        public Entity JoinPointB;
        public Entity Profile;

        private static GameObject prefab;
        public float3 BoundingBoxSize;

        public static GameObject Prefab =>
            prefab == null
                ? prefab = Resources.Load<GameObject>("prefabs/ecsLine")
                : prefab;

        public static Entity New(float3 position, GameObject prefab)
        {
            var result = LineWorld.Em.CreateEntity(LineArchetype);
            LineWorld.Em.SetComponentData(result,
                                          new Line
                                          {
                                              Position = position,
                                              Profile  = Entity.Null
                                          });
            LineWorld.Em.AddComponentObject(result,
                                            Object.Instantiate(prefab, position, Quaternion.identity)
                                                  .GetComponent<EcsLineBehaviour>());
            return result;
        }
    }
}