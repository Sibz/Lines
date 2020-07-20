using Sibz.Lines.ECS.Components;
using Sibz.Lines.ECS.Systems;
using Sibz.Math;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Sibz.Lines.ECS.Jobs
{
    public class NewLineHeightMapChangeSystem : SystemBase
    {
        private NativeHashMap<int, float>               actualHeightData;
        private NativeHashMap<int, float2>              minMaxData;
        public  NativeHashMap<int, float>               FilteredHeightData;
        private EntityQuery                             heightMapChangeQuery;
        private NativeQueue<LineTerrainMinMaxHeightMap> minMaxDataToUpdateSynchronously;


        protected override void OnCreate()
        {
            heightMapChangeQuery =
                GetEntityQuery(new EntityQueryDesc
                               {
                                   All = new[]
                                         {
                                             ComponentType.ReadOnly<HeightMapChange>(),
                                             ComponentType.ReadOnly<LineTerrainMinMaxHeightMap>(),
                                         }
                               });
            RequireForUpdate(heightMapChangeQuery);
        }

        public void FirstRun()
        {
            var td      = Terrain.activeTerrain.terrainData;
            var heights = td.GetHeights(0, 0, td.heightmapResolution, td.heightmapResolution);
            actualHeightData = new NativeHashMap<int, float>(heights.GetLength(0) * heights.GetLength(1),
                                                             Allocator.Persistent);
            minMaxData = new NativeHashMap<int, float2>(heights.GetLength(0) * heights.GetLength(1),
                                                        Allocator.Persistent);
            FilteredHeightData = new NativeHashMap<int, float>(heights.GetLength(0) * heights.GetLength(1),
                                                               Allocator.Persistent);
            for (int x = 0; x < heights.GetLength(0); x++)
            for (int y = 0; y < heights.GetLength(1); y++)
            {
                actualHeightData.Add(new int2(x, y).GetHashCode(), heights[x, y]);
                FilteredHeightData.Add(new int2(x, y).GetHashCode(), heights[x, y]);
            }
        }

        protected override void OnUpdate()
        {
            Dispose();
            var lineEntities = heightMapChangeQuery.ToEntityArrayAsync(Allocator.TempJob, out var jh1);
            var count        = lineEntities.Length;
            minMaxDataToUpdateSynchronously = new NativeQueue<LineTerrainMinMaxHeightMap>(Allocator.TempJob);
            Dependency = new HeightMapAddMinMaxDataJob
                         {
                             MinMaxData                      = minMaxData.AsParallelWriter(),
                             MinMaxDataToUpdateSynchronously = minMaxDataToUpdateSynchronously.AsParallelWriter(),
                             LineEntities                    = lineEntities,
                             LineHeightMaps                  = GetBufferFromEntity<LineTerrainMinMaxHeightMap>()
                         }.Schedule(count, 4, JobHandle.CombineDependencies(Dependency, jh1));
            Dependency = new HeightMapUpdateMinMaxDataJob
                         {
                             MinMaxData                      = minMaxData,
                             MinMaxDataToUpdateSynchronously = minMaxDataToUpdateSynchronously
                         }.Schedule(Dependency);

            Dependency = new UpdateFilteredData
                         {
                             LineEntities       = lineEntities,
                             LineHeightMaps     = GetBufferFromEntity<LineTerrainMinMaxHeightMap>(true),
                             MinMaxData         = minMaxData,
                             ActualHeightData   = actualHeightData,
                             FilteredHeightData = FilteredHeightData,
                             Changes            = GetComponentDataFromEntity<HeightMapChange>(),
                         }.Schedule(Dependency);

            Dependency = new TriggerUpdateTerrainHeightMapJob
                         {
                             Ecb          = LineEndSimBufferSystem.Instance.CreateCommandBuffer().ToConcurrent(),
                             LineEntities = lineEntities,
                             Changes      = GetComponentDataFromEntity<HeightMapChange>(),
                         }.Schedule(count, 4, Dependency);

            Dependency = new DeallocateJob<Entity>
                         {
                             NativeArray1 = lineEntities
                         }.Schedule(Dependency);

            LineEndSimBufferSystem.Instance.AddJobHandleForProducer(Dependency);

            LineEndSimBufferSystem.Instance.CreateCommandBuffer()
                                  .RemoveComponent<HeightMapChange>(heightMapChangeQuery);
        }

        protected override void OnStopRunning()
        {
            Dispose();
        }

        private void Dispose()
        {
            if (minMaxDataToUpdateSynchronously.IsCreated)
            {
                minMaxDataToUpdateSynchronously.Dispose();
            }
        }

        protected override void OnDestroy()
        {
            minMaxData.Dispose();
            actualHeightData.Dispose();
            FilteredHeightData.Dispose();
        }

        [BurstCompile]
        public struct TriggerUpdateTerrainHeightMapJob : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<Entity> LineEntities;

            [ReadOnly]
            public ComponentDataFromEntity<HeightMapChange> Changes;

            public EntityCommandBuffer.Concurrent Ecb;

            public void Execute(int index)
            {
                var ent = Ecb.CreateEntity(index);
                Ecb.AddComponent(index, ent, new HeightMapUpdateTrigger
                                             {
                                                 Size          = Changes[LineEntities[index]].Size,
                                                 StartPosition = Changes[LineEntities[index]].StartPosition
                                             });
            }
        }

        [BurstCompile]
        public struct UpdateFilteredData : IJob
        {
            [ReadOnly]
            public NativeHashMap<int, float2> MinMaxData;

            [ReadOnly]
            public NativeArray<Entity> LineEntities;

            [ReadOnly]
            public NativeHashMap<int, float> ActualHeightData;

            public NativeHashMap<int, float> FilteredHeightData;

            [ReadOnly]
            public ComponentDataFromEntity<HeightMapChange> Changes;

            [ReadOnly]
            public BufferFromEntity<LineTerrainMinMaxHeightMap> LineHeightMaps;

            public void Execute()
            {
                for (int i = 0; i < LineEntities.Length; i++)
                {
                    //var buff = LineHeightMaps[LineEntities[i]];
                    Update(Changes[LineEntities[i]], LineHeightMaps[LineEntities[i]]);
                }
            }

            public void Update(HeightMapChange change, DynamicBuffer<LineTerrainMinMaxHeightMap> buff)
            {
                for (int x = 0; x < change.Size.x; x++)
                for (int y = 0; y < change.Size.y; y++)
                {
                    //Debug.LogFormat("x:{0}, y{1}", x + change.StartPosition.x, y + change.StartPosition.y);
                    var hashCode = new int2(x + change.StartPosition.x, y + change.StartPosition.y).GetHashCode();
                    if (MinMaxData.ContainsKey(hashCode))
                        FilteredHeightData[hashCode] =
                            math.clamp(ActualHeightData[hashCode], MinMaxData[hashCode].x, MinMaxData[hashCode].y);
                }
                /*{

                }
                for (int i = 0; i < buff.Length; i++)
                {
                    var hashCode = buff[i].Position.GetHashCode();
                    FilteredHeightData[hashCode] =
                        math.clamp(ActualHeightData[hashCode], MinMaxData[hashCode].x, MinMaxData[hashCode].y);
                    /*if (MinMaxData[hashCode].y>0.01f)
                    {
                        Debug.Log(MinMaxData[hashCode].y);
                    }#1#
                }*/

                /*var count = 0;
                for (int x = heightMapChange.StartPosition.x;
                     x < heightMapChange.StartPosition.x + heightMapChange.Size.x;
                     x++)
                for (int y = heightMapChange.StartPosition.y;
                     y < heightMapChange.StartPosition.y + heightMapChange.Size.y;
                     y++)
                {
                    var hashCode = new int2(x, y).GetHashCode();
                    if (MinMaxData.ContainsKey(hashCode))
                    {
                        FilteredHeightData[hashCode] =
                            math.clamp(ActualHeightData[hashCode], MinMaxData[hashCode].x, MinMaxData[hashCode].y);
                    }
                }*/
            }
        }

        [BurstCompile]
        public struct HeightMapAddMinMaxDataJob : IJobParallelFor
        {
            public NativeHashMap<int, float2>.ParallelWriter              MinMaxData;
            public NativeQueue<LineTerrainMinMaxHeightMap>.ParallelWriter MinMaxDataToUpdateSynchronously;

            [ReadOnly]
            public NativeArray<Entity> LineEntities;

            [ReadOnly]
            public BufferFromEntity<LineTerrainMinMaxHeightMap> LineHeightMaps;

            public void Execute(int index)
            {
                var buffer = LineHeightMaps[LineEntities[index]];
                var len    = buffer.Length;
                for (int i = 0; i < len; i++)
                {
                    if (!MinMaxData.TryAdd(buffer[i].Position.GetHashCode(),
                                           new float2(buffer[i].Min, buffer[i].Max)))
                    {
                        MinMaxDataToUpdateSynchronously.Enqueue(buffer[i]);
                    }
                }
            }
        }

        [BurstCompile]
        public struct HeightMapUpdateMinMaxDataJob : IJob
        {
            public NativeHashMap<int, float2>              MinMaxData;
            public NativeQueue<LineTerrainMinMaxHeightMap> MinMaxDataToUpdateSynchronously;

            public void Execute()
            {
                while (MinMaxDataToUpdateSynchronously.TryDequeue(out var item))
                {
                    MinMaxData[item.Position.GetHashCode()] = new float2(item.Min, item.Max);
                }
            }
        }
    }
}