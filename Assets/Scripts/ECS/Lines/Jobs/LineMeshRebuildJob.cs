using Sibz.Lines.ECS.Components;
using Unity.Entities;
using Unity.Mathematics;

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
            {
                return;
            }

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

                VertexData.Add(new MeshVertexData
                {
                    Position = new float3(knot.x - xMod, knot.y, knot.z - zMod),
                    Normal = new float3(0,1,0),
                    Uv = new float2(0, (float) i / (Knots.Length - 1))
                });
                VertexData.Add(new MeshVertexData
                {
                    Position = new float3(knot.x + xMod, knot.y, knot.z + zMod),
                    Normal = new float3(0,1,0),
                    Uv = new float2(0, (float) i / (Knots.Length - 1))
                });

                if (i == Knots.Length - 1)
                {
                    continue;
                }

                Triangles.Add(new MeshTriangleData { VertexIndex = vertexIndex1 + 1 });
                Triangles.Add(new MeshTriangleData { VertexIndex = vertexIndex1 + 3 });
                Triangles.Add(new MeshTriangleData { VertexIndex = vertexIndex1 + 2 });
                Triangles.Add(new MeshTriangleData { VertexIndex = vertexIndex1 + 1 });
                Triangles.Add(new MeshTriangleData { VertexIndex = vertexIndex1 + 2 });
                Triangles.Add(new MeshTriangleData { VertexIndex = vertexIndex1 });
            }
        }
    }
}