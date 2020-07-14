using Unity.Entities;
using Unity.Mathematics;

namespace Sibz.Lines.ECS.Components
{
    public struct LineToolData
    {
        public ToolModifiers Modifiers;
        public Entity        LineProfileEntity;
        public Entity        LineEntity;
        public float3x3      Bezier1;
        public float3x3      Bezier2;

        public struct ToolModifiers
        {
            public EndMods To;
            public EndMods From;

            public struct EndMods
            {
                public float Size;
                public float Ratio;
                public float Height;
                public float InnerHeight;
                public float InnerHeightDistanceFromEnd;
            }
        }
    }
}