using Sibz.Lines.ECS.Components;
using Unity.Entities;
using Unity.Mathematics;

namespace Sibz.Lines.ECS.Jobs
{
    public struct LineMeshRebuildJob
    {
        public DynamicBuffer<LineKnotData>     Knots;
        public DynamicBuffer<MeshTriangleData> Triangles;
        public DynamicBuffer<MeshVertexData>   VertexData;
        public float3x2                        EndDirections;
        public LineProfile                     Profile;

        public void Execute()
        {
            Triangles.Clear();
            VertexData.Clear();
            if (Knots.Length == 0) return;

            var up        = new float3(0, 1, 0);
            var halfWidth = Profile.Width / 2f;
            for (var i = 0; i < Knots.Length; i++)
            {
                var knot = Knots[i].Position;
                var direction = i == 0                ? -EndDirections.c0 :
                                i == Knots.Length - 1 ? EndDirections.c1 :
                                                        knot - Knots[i + 1].Position;
                var cross = math.normalize(math.cross(up, direction));
                var zMod  = cross.z * halfWidth;
                var xMod  = cross.x * halfWidth;


                VertexData.Add(new MeshVertexData
                               {
                                   Position = new float3(knot.x - xMod, knot.y, knot.z - zMod),
                                   Normal   = new float3(0, 1, 0),
                                   Uv       = new float2(0, (float) i / (Knots.Length - 1))
                               });

                VertexData.Add(new MeshVertexData
                               {
                                   Position = new float3(knot.x + xMod, knot.y, knot.z + zMod),
                                   Normal   = new float3(0, 1, 0),
                                   Uv       = new float2(0, (float) i / (Knots.Length - 1))
                               });
                var vertexIndex1 = VertexData.Length - 1;

                if (i == 0) continue;

                Triangles.Add(new MeshTriangleData {VertexIndex = vertexIndex1 - 2});
                Triangles.Add(new MeshTriangleData {VertexIndex = vertexIndex1});
                Triangles.Add(new MeshTriangleData {VertexIndex = vertexIndex1 - 1});
                Triangles.Add(new MeshTriangleData {VertexIndex = vertexIndex1 - 2});
                Triangles.Add(new MeshTriangleData {VertexIndex = vertexIndex1 - 1});
                Triangles.Add(new MeshTriangleData {VertexIndex = vertexIndex1 - 3});
            }
        }
    }
}