using Unity.Entities;

namespace Sibz.Lines.ECS.Components
{
    public struct LineToolData
    {
        public float FromPosition;
        public float ToPosition;
        public LineToolDataModifiers Modifiers;
        public Entity LineEntity;
        public Entity FromJoinPointEntity;
        public Entity ToJoinPointEntity;
    }
}