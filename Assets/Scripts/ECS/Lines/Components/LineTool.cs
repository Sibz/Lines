using Sibz.Lines.ECS.Enums;
using Unity.Entities;

namespace Sibz.Lines.ECS.Components
{
    public struct LineTool : IComponentData
    {
        public LineToolState State;
    }
}