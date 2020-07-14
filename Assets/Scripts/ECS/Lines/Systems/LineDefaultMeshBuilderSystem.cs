using Sibz.Lines.ECS.Components;
using Sibz.Lines.ECS.Jobs;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Sibz.Lines.ECS.Systems
{
    [UpdateInGroup(typeof(LineWorldPresGroup), OrderFirst = true)]
    public class LineDefaultMeshBuilderSystem : SystemBase
    {
        private EntityQuery meshJobQuery;
        private EntityQuery lineQuery;
        private EntityQuery lineJoinsQuery;

        public static Entity Prefab;

        protected override void OnCreate()
        {
            meshJobQuery = GetEntityQuery(typeof(MeshBuildData), typeof(DefaultMeshBuilder));

            lineQuery = GetEntityQuery(typeof(Line));

            lineJoinsQuery = GetEntityQuery(typeof(LineJoinPoint));

            RequireForUpdate(meshJobQuery);

            Prefab = EntityManager.CreateEntity(typeof(MeshBuildData), typeof(DefaultMeshBuilder), typeof(Prefab));
        }

        protected override void OnUpdate()
        {
            var triangleBuffer = GetBufferFromEntity<MeshTriangleData>();
            var vertexBuffer = GetBufferFromEntity<MeshVertexData>();
            var knotBuffer = GetBufferFromEntity<LineKnotData>(true);
            var defaultLineProfile = LineProfile.Default();

            var lineEntities = lineQuery.ToEntityArrayAsync(Allocator.TempJob, out JobHandle jh1);
            var lines = lineQuery.ToComponentDataArrayAsync<Line>(Allocator.TempJob, out JobHandle jh2);
            Dependency = JobHandle.CombineDependencies(Dependency, jh1, jh2);

            var joinEntities = lineJoinsQuery.ToEntityArrayAsync(Allocator.TempJob, out jh1);
            var joinPoints = lineJoinsQuery.ToComponentDataArrayAsync<LineJoinPoint>(Allocator.TempJob, out jh2);
            Dependency = JobHandle.CombineDependencies(Dependency, jh1, jh2);

            var ecb = LineEndSimBufferSystem.Instance.CreateCommandBuffer().ToConcurrent();
            Dependency = Entities
                .WithDeallocateOnJobCompletion(lineEntities)
                .WithDeallocateOnJobCompletion(lines)
                .WithDeallocateOnJobCompletion(joinEntities)
                .WithDeallocateOnJobCompletion(joinPoints)
                .ForEach((Entity entity, int entityInQueryIndex, ref MeshBuildData data) =>
                {
                    ecb.DestroyEntity(entityInQueryIndex, entity);
                    if (!lineEntities.Contains(data.LineEntity))
                    {
                        return;
                    }

                    Line line = lines[lineEntities.IndexOf<Entity>(data.LineEntity)];
                    LineJoinPoint joinPointA = joinPoints[joinEntities.IndexOf<Entity>(line.JoinPointA)];
                    LineJoinPoint joinPointB = joinPoints[joinEntities.IndexOf<Entity>(line.JoinPointB)];

                    new LineMeshRebuildJob
                    {
                        Knots = knotBuffer[data.LineEntity],
                        Triangles = triangleBuffer[data.LineEntity],
                        VertexData = vertexBuffer[data.LineEntity],
                        // TODO: Load line profile
                        Profile = defaultLineProfile,
                        EndDirections = new float3x2(joinPointA.Direction, joinPointB.Direction)
                    }.Execute();
                    ecb.AddComponent<MeshUpdated>(entityInQueryIndex, data.LineEntity);

                }).Schedule(Dependency);
            LineEndSimBufferSystem.Instance.AddJobHandleForProducer(Dependency);
        }
    }
}