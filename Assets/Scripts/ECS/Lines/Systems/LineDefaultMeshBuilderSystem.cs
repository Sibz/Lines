using Sibz.Lines.ECS.Components;
using Sibz.Lines.ECS.Jobs;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Sibz.Lines.ECS.Systems
{
    public class LineDefaultMeshBuilderSystem : SystemBase
    {
        private EntityQuery meshJobQuery;
        private EntityQuery lineQuery;
        private EntityQuery lineJoinsQuery;

        protected override void OnCreate()
        {
            meshJobQuery = GetEntityQuery(typeof(MeshBuildData), typeof(DefaultMeshBuilder));

            lineQuery = GetEntityQuery(typeof(Line));

            lineJoinsQuery = GetEntityQuery(typeof(LineJoinPoint));

            RequireForUpdate(meshJobQuery);
        }

        protected override void OnUpdate()
        {
            var triangleBuffer = GetBufferFromEntity<MeshTriangleData>();
            var vertexBuffer = GetBufferFromEntity<MeshVertexData>();
            var knotBuffer = GetBufferFromEntity<LineKnotData>(true);
            var defaultLineProfile = LineProfile.Default();
            JobHandle jh1, jh2;
            var lineEntities = lineQuery.ToEntityArrayAsync(Allocator.TempJob, out jh1);
            var lines = lineQuery.ToComponentDataArrayAsync<Line>(Allocator.TempJob, out jh2);
            Dependency = JobHandle.CombineDependencies(Dependency, jh1, jh2);
            var joinEntities = lineJoinsQuery.ToEntityArrayAsync(Allocator.TempJob, out jh1);
            var joinPoints = lineJoinsQuery.ToComponentDataArrayAsync<LineJoinPoint>(Allocator.TempJob, out jh2);
            Dependency = JobHandle.CombineDependencies(Dependency, jh1, jh2);
            //var lines =
            Entities
                .WithDeallocateOnJobCompletion(lineEntities)
                .WithDeallocateOnJobCompletion(lines)
                .WithDeallocateOnJobCompletion(joinEntities)
                .WithDeallocateOnJobCompletion(joinPoints)
                .ForEach((Entity entity, ref MeshBuildData data) =>
                {
                    var line = joinPoints[lineEntities.IndexOf<Entity>(data.LineEntity)];

                    new LineMeshRebuildJob
                    {
                        Knots = knotBuffer[data.LineEntity],
                        Triangles = triangleBuffer[data.LineEntity],
                        VertexData = vertexBuffer[data.LineEntity],
                        // TODO: Load line profile
                        Profile = defaultLineProfile,
                        EndDirections =
                    }
                }).Schedule(Dependency);
        }
    }
}