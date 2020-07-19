using System;
using Unity.Mathematics;
using UnityEngine;

namespace Sibz.Lines.ECS.Jobs
{
    public class NewLineGetTerrainHeightMapForBoundsJob
    {
        // This isn't really a job, we're just getting height data from managed space
        // using the same job style
        public float3x2 Bounds;


        public void Execute()
        {
            var     terrains = Terrain.activeTerrains;
            Terrain terrain  = null;
            for (int i = 0; i < terrains.Length; i++)
            {
                var terrainBounds = terrains[i].terrainData.bounds;
                if (terrainBounds.Contains(Bounds.c0))
                    terrain = terrains[i];
            }

            if (terrain == null)
                throw new Exception("Terrain not found");

            var td = terrain.terrainData;
            var terrainBoundsXStart = (int)math.round(((Bounds.c0.x - Bounds.c1.x / 2)
                                     - (terrain.transform.position.x / td.size.x))
                                    * td.heightmapResolution);
            var terrainBoundsYStart = (int)math.round(((Bounds.c0.y - Bounds.c1.y / 2)
                                                     - (terrain.transform.position.y / td.size.y))
                                                    * td.heightmapResolution);
            var heights = td.GetHeights(terrainBoundsXStart, terrainBoundsYStart,
                          (int)math.round(Bounds.c1.x / td.size.x * td.heightmapResolution),
                          (int)math.round(Bounds.c1.y / td.size.y * td.heightmapResolution));

        }
    }
}