using Unity.Mathematics;

namespace Sibz.Lines.ECS.Components
{
    public struct NewLineModifiers
    {
        /// <summary>
        /// End heights for each end point on a new line
        /// </summary>
        public float2 EndHeights;
        /// <summary>
        /// Inner heights are used to create arcs or bridge like height adjustments
        /// They are essentially the control points in the height bezier
        /// </summary>
        public float2 InnerHeights;
    }
}