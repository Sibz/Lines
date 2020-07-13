using Unity.Entities;

namespace Sibz.Lines.ECS.Components
{
    public struct DefaultMeshBuilder : IComponentData
    {
        private static EntityArchetype Archetype = LineWorld.Em
            .CreateArchetype(typeof(MeshBuildData), typeof(DefaultMeshBuilder));
        private static Entity prefab;
        public static Entity Prefab => LineWorld.Em.Exists(prefab)
            ? prefab
            : prefab = LineWorld.Em.CreateEntity(Archetype);
    }
}