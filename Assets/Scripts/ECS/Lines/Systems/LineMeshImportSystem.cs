using Sibz.Lines.ECS.Behaviours;
using Sibz.Lines.ECS.Components;
using Unity.Entities;
using UnityEngine;

namespace Sibz.Lines.ECS.Systems
{
    [UpdateInGroup(typeof(LineWorldPresGroup))]
    public class LineMeshImportSystem : SystemBase
    {
        private EntityQuery query;

        protected override void OnCreate()
        {
            query = GetEntityQuery(typeof(Line), typeof(MeshUpdated));
        }

        protected override void OnUpdate()
        {
            Entities
                .WithStructuralChanges()
                .WithAll<MeshUpdated>()
                .ForEach((Entity lineEntity, DynamicBuffer<MeshTriangleData> triangleData, DynamicBuffer<MeshVertexData> vertexData) =>
            {
                var lineBehaviour = EntityManager.GetComponentObject<EcsLineBehaviour>(lineEntity);
                Mesh mesh = new Mesh();
                int len = vertexData.Length;
                Vector3[] vertices = new Vector3[len];
                Vector3[] normals = new Vector3[len];
                Vector2[] uvs = new Vector2[len];
                int[] triangles = new int[triangleData.Length];

                for (int i = 0; i < len; i++)
                {
                    vertices[i] = lineBehaviour.transform.InverseTransformPoint(vertexData[i].Position);
                    normals[i] = vertexData[i].Normal;
                    uvs[i] = vertexData[i].Uv;
                }

                for (int i = 0; i < triangleData.Length; i++)
                {
                    triangles[i] = triangleData[i].VertexIndex;
                }

                mesh.vertices = vertices;
                mesh.normals = normals;
                mesh.uv = uvs;
                mesh.triangles = triangles;
                lineBehaviour.MeshFilter.sharedMesh = mesh;
                EntityManager.RemoveComponent<MeshUpdated>(lineEntity);
            }).WithoutBurst().Run();

        }
    }
}