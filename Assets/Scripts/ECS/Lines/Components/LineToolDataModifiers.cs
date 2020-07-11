namespace Sibz.Lines.ECS.Components
{
    public struct LineToolDataModifiers
    {
        public float MaxAngularChange;
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