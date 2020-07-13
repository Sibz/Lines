using Sibz.Lines.ECS.Components;
using Unity.Entities;

namespace Sibz.Lines.ECS.Events
{
    public struct LineToolModChangeEvent :IComponentData
    {
        public LineToolData.ToolModifiers ModifierChangeValues;

        public static void New(LineToolData.ToolModifiers modifierChangeValues)
        {
            var ent = LineWorld.Em.CreateEntity();
            LineWorld.Em.AddComponentData(ent, new LineToolModChangeEvent
            {
                ModifierChangeValues = modifierChangeValues
            });
        }
    }
}