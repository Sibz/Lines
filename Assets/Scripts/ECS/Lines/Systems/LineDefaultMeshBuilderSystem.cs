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
        public static Entity      Prefab;
        private       EntityQuery lineJoinsQuery;
        private       EntityQuery lineQuery;
        private       EntityQuery meshJobQuery;

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
            var triangleBuffer     = GetBufferFromEntity<MeshTriangleData>();
            var vertexBuffer       = GetBufferFromEntity<MeshVertexData>();
            var knotBuffer         = GetBufferFromEntity<LineKnotData>(true);
            var defaultLineProfile = LineProfile.Default();

            var lineEntities = lineQuery.ToEntityArrayAsync(Allocator.TempJob, out var jh1);
            var lines        = lineQuery.ToComponentDataArrayAsync<Line>(Allocator.TempJob, out var jh2);
            Dependency = JobHandle.CombineDependencies(Dependency, jh1, jh2);

            var joinEntities = lineJoinsQuery.ToEntityArrayAsync(Allocator.TempJob, out jh1);
            var joinPoints =
                lineJoinsQuery.ToComponentDataArrayAsync<LineJoinPoint>(Allocator.TempJob, out jh2);
            Dependency = JobHandle.CombineDependencies(Dependency, jh1, jh2);

            var ecb = LineEndSimBufferSystem.Instance.CreateCommandBuffer().AsParallelWriter();
            Dependency = Entities
                        .WithDisposeOnCompletion(lineEntities)
                        .WithDisposeOnCompletion(lines)
                        .WithDisposeOnCompletion(joinEntities)
                        .WithDisposeOnCompletion(joinPoints)
                        .ForEach((Entity entity, int entityInQueryIndex, ref MeshBuildData data) =>
                                 {
                                     ecb.DestroyEntity(entityInQueryIndex, entity);
                                     if (!lineEntities.Contains(data.LineEntity)) return;

                                     var line = lines[lineEntities.IndexOf<Entity>(data.LineEntity)];
                                     var joinPointA =
                                         joinPoints[joinEntities.IndexOf<Entity>(line.JoinPointA)];
                                     var joinPointB =
                                         joinPoints[joinEntities.IndexOf<Entity>(line.JoinPointB)];

                                     new LineMeshRebuildJob
                                     {
                                         Knots      = knotBuffer[data.LineEntity],
                                         Triangles  = triangleBuffer[data.LineEntity],
                                         VertexData = vertexBuffer[data.LineEntity],
                                         // TODO: Load line profile
                                         Profile       = defaultLineProfile,
                                         EndDirections = new float3x2(joinPointA.Direction, joinPointB.Direction)
                                     }.Execute();
                                     ecb.AddComponent<MeshUpdated>(entityInQueryIndex, data.LineEntity);
                                 }).Schedule(Dependency);
            LineEndSimBufferSystem.Instance.AddJobHandleForProducer(Dependency);
        }
    }
}