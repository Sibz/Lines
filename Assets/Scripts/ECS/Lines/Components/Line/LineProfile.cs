using Unity.Entities;
using Unity.Mathematics;

namespace Sibz.Lines.ECS.Components
{
    public struct LineProfile : IComponentData
    {
        public float  MaxCurveAngularChange;
        public float  MaxGradientAngularChange;
        public float  CollisionWidth;
        public float  CollisionHeight;
        public float  CollisionDepth;
        public float  KnotSpacing;
        public Entity MeshBuildPrefab;
        public float  Width;
        public ProfileTerrainConstraints TerrainConstraints;

        public struct ProfileTerrainConstraints
        {
            public float SideCuttingAngle;
            public float EndCuttingAngle;
            public float SideRiseAngle;
            public float EndRiseAngle;
            public float MaxDepth;
            public float MaxRise;
        }

        public static LineProfile Default()
        {
            const float defaultAngleInRadians = math.PI / 3.5f;
            return new LineProfile
                   {
                       Width           = 1f,
                       KnotSpacing     = 0.25f,
                       MeshBuildPrefab = Entity.Null,
                       TerrainConstraints =
                       {
                           MaxDepth = 5,
                           MaxRise = 5,
                           SideCuttingAngle = defaultAngleInRadians,
                           EndCuttingAngle = defaultAngleInRadians,
                           SideRiseAngle = defaultAngleInRadians,
                           EndRiseAngle = defaultAngleInRadians
                       }
                   };
        }
    }
}