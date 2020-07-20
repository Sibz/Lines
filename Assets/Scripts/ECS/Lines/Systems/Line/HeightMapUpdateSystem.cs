using Sibz.Lines.ECS.Components;
using Sibz.Lines.ECS.Jobs;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

namespace Sibz.Lines.ECS.Systems
{
    [UpdateInGroup(typeof(LineWorldPresGroup))]
    public class HeightMapUpdateSystem : SystemBase
    {
        private EntityQuery query;

        protected override void OnUpdate()
        {

            // https://forum.unity.com/threads/understanding-copyactiverendertexturetoheightmap.706679/#post-4733732
            Entities
               .WithStoreEntityQueryInField(ref query)
               .WithoutBurst()
               .ForEach((ref HeightMapUpdateTrigger item) =>
                        {
                            float[,] heights = new float[item.Size.y + 1, item.Size.x + 1];
                            var      h = World.GetExistingSystem<NewLineHeightMapChangeSystem>().FilteredHeightData;
                            Profiler.BeginSample("A");
                            for (int x = 0; x <= item.Size.x; x++)
                            for (int y = 0; y <= item.Size.y; y++)
                            {
                                var hashCode =
                                    new int2(x + item.StartPosition.x, y + item.StartPosition.y).GetHashCode();
//                                if (h.ContainsKey(hashCode))
                                    heights[y, x] = h[hashCode];

                            }
                            Profiler.EndSample();

                            Profiler.BeginSample("B");
                            Terrain.activeTerrain.terrainData.SetHeights(item.StartPosition.x,
                                                                                 item.StartPosition.y, heights);
                            Profiler.EndSample();

                        }).Run();
            EntityManager.DestroyEntity(query);
        }
    }
}