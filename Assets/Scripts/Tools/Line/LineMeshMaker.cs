using System;
using Unity.Mathematics;
using UnityEngine;

namespace Sibz.Lines
{
    public static class LineMeshMaker
    {
        public static Mesh Build(float3 originForward, float3 endForward, float3[] knots, float width)
        {
            Mesh result = new Mesh();
            Vector3[] vertices = new Vector3[knots.Length * 2];
            int[] triangles = new int[(knots.Length - 1) * 6];
            Vector3[] normals = new Vector3[knots.Length * 2];
            Vector2[] uvs = new Vector2[knots.Length * 2];
            float3 up = new float3(0, 1, 0);
            float halfWidth = width / 2f;
            for (int i = 0; i < knots.Length; i++)
            {
                float3 knot = knots[i];
                float3 direction = i == 0 ? -originForward :
                    i == knots.Length - 1 ? endForward :
                    knot - knots[i + 1];

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
                uvs[vertexIndex1] = new Vector2(0, (float) i / (knots.Length - 1));
                uvs[vertexIndex1] = new Vector2(1, (float) i / (knots.Length - 1));
                if (i != knots.Length - 1)
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

            result.vertices = vertices;
            result.triangles = triangles;
            result.normals = normals;
            result.uv = uvs;
            return result;
        }


        public static Mesh Build(Vector3[] linePoints, float width, int sections)
        {
            if (linePoints.Length < 2)
            {
                throw new ArgumentException("Should have 2 or more vectors", nameof(linePoints));
            }

            if (linePoints.Length == 2 && sections == 1)
            {
                return BuildQuad(linePoints[0], linePoints[1], width);
            }

            Mesh result = new Mesh();
            return new Mesh();
        }

        private static Mesh BuildQuad(Vector3 point1, Vector3 point2, float width)
        {
            Mesh mesh = new Mesh();
            float halfWidth = width / 2f;

            Vector3 up = Vector3.up;
            Vector3 cross = Vector3.Cross(up, point2 - point1).normalized;
            float zMod = cross.z * halfWidth;
            float xMod = cross.x * halfWidth;

            mesh.vertices = new[]
            {
                new Vector3(point1.x - xMod, point1.y, point1.z - zMod),
                new Vector3(point1.x + xMod, point1.y, point1.z + zMod),
                new Vector3(point2.x - xMod, point2.y, point2.z - zMod),
                new Vector3(point2.x + xMod, point2.y, point2.z + zMod),
            };

            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            mesh.normals = new[] { up, up, up, up };
            mesh.uv = new[]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(1, 1)
            };
            return mesh;
        }
    }
}