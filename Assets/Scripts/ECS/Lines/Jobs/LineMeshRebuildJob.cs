using Sibz.Lines.ECS.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Sibz.Lines.ECS.Jobs
{
    public struct LineMeshRebuildJob
    {
        public DynamicBuffer<LineKnotData> Knots;
        public DynamicBuffer<MeshTriangleData> Triangles;
        public DynamicBuffer<MeshVertexData> VertexData;
        public float3x2 EndDirections;
        public LineProfile Profile;

        public void Execute()
        {
            Triangles.Clear();
            VertexData.Clear();
            if (Knots.Length == 0)
                return;

            Vector3[] vertices = new Vector3[Knots.Length * 2];
            int[] triangles = new int[(Knots.Length - 1) * 6];
            Vector3[] normals = new Vector3[Knots.Length * 2];
            Vector2[] uvs = new Vector2[Knots.Length * 2];
            float3 up = new float3(0, 1, 0);
            float halfWidth = Profile.Width / 2f;
            for (int i = 0; i < Knots.Length; i++)
            {
                float3 knot = Knots[i].Position;
                float3 direction = i == 0 ? -EndDirections.c0 :
                    i == Knots.Length - 1 ? EndDirections.c1 :
                    knot - Knots[i + 1].Position;

                float3 cross = math.normalize(math.cross(up, direction));

                float zMod = cross.z * halfWidth;
                float xMod = cross.x * halfWidth;
                int vertexIndex1 = i * 2;
                int vertexIndex2 = i * 2 + 1;
                vertices[vertexIndex1] =
                    new Vector3(knot.x - xMod, knot.y, knot.z - zMod);
                vertices[vertexIndex2] =
                    new Vector3(knot.x + xMod, knot.y, knot.z + zMod);
                normals[vertexIndex1] = Vector3.up;
                normals[vertexIndex2] = Vector3.up;
                uvs[vertexIndex1] = new Vector2(0, (float) i / (Knots.Length - 1));
                uvs[vertexIndex1] = new Vector2(1, (float) i / (Knots.Length - 1));
                if (i != Knots.Length - 1)
                {
                    int triIndex = i * 6;
                    triangles[triIndex] = vertexIndex1 + 1;
                    triangles[triIndex + 1] = vertexIndex1 + 3;
                    triangles[triIndex + 2] = vertexIndex1 + 2;
                    triangles[triIndex + 3] = vertexIndex1 + 1;
                    triangles[triIndex + 4] = vertexIndex1 + 2;
                    triangles[triIndex + 5] = vertexIndex1;
                }
            }
        }
    }
}