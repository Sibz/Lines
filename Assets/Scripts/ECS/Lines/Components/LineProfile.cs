using Unity.Entities;

namespace Sibz.Lines.ECS.Components
{
    public struct LineProfile : IComponentData
    {
        public float MaxCurveAngularChange;
        public float MaxGradientAngularChange;
        public float CollisionWidth;
        public float CollisionHeight;
        public float CollisionDepth;
        public float KnotSpacing;
        public Entity MeshBuildPrefab;
        public float Width;

        public static LineProfile Default()
        {
            return new LineProfile
            {
                Width = 1f,
                KnotSpacing = 0.25f,
                MeshBuildPrefab = Entity.Null
            };
        }
    }
}