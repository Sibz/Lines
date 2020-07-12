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

        public static LineProfile Default()
        {
            return new LineProfile
            {
                KnotSpacing = 0.25f
            };
        }
    }
}