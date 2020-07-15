using Unity.Entities;
using UnityEngine;

namespace Sibz.Lines.ECS.Systems
{
    [UpdateInGroup(typeof(LineWorldSimGroup), OrderLast = true), ExecuteAlways]
    public class LineEndSimBufferSystem : EntityCommandBufferSystem
    {
        public static LineEndSimBufferSystem Instance => LineWorld.World.GetExistingSystem<LineEndSimBufferSystem>();
    }
}