using Sibz.Lines.ECS.Components;
using Sibz.Lines.ECS.Jobs;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Sibz.Lines.ECS.Systems
{
    public class HeightMapUpdateSystem : SystemBase
    {
        private EntityQuery query;

        protected override void OnUpdate()
        {
            Entities
               .WithStoreEntityQueryInField(ref query)
               .WithoutBurst()
               .ForEach((ref HeightMapUpdateTrigger item) =>
                        {
                            float[,] heights = new float[item.Size.x, item.Size.y];
                            var      h = World.GetExistingSystem<NewLineHeightMapChangeSystem>().FilteredHeightData;
                            for (int x = 0; x < item.Size.x; x++)
                            for (int y = 0; y < item.Size.y; y++)
                            {
                                var hashCode =
                                    new int2(x + item.StartPosition.x, y + item.StartPosition.y).GetHashCode();
//                                if (h.ContainsKey(hashCode))
                                    heights[x, y] = h[hashCode];

                            }

                            Terrain.activeTerrain.terrainData.SetHeightsDelayLOD(item.StartPosition.x,
                                                                                 item.StartPosition.y, heights);
                        }).Run();
            EntityManager.DestroyEntity(query);
        }
    }
}