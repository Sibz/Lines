using System;
using Sibz.Lines.ECS.Components;
using Sibz.Lines.ECS.Systems;
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
        private NativeArray<float>                           actualHeightMapData;
        private NativeArray<float>                           filteredHeightMapData;
        public  NativeHashMap<int, float>                    ActualHeightData;
        public  NativeHashMap<int, float2>                   MinMaxData;
        public  NativeHashMap<Entity, int2x2>                EntityMapBounds;
        public  NativeMultiHashMap<int, EntityAndMinMaxData> EntityMinMaxData;
        public  NativeMultiHashMap<Entity, int>              EntityMinMaxEntryIndexes;
        public  NativeHashMap<int, float>                    FilteredHeightData;
        public  NativeQueue<int>                             IndexesModifiedQueue;
        private NativeList<int>                              removedIndexes;
        private NativeList<int>                              modifiedIndexes;

        private EntityQuery heightMapChangeQuery;

        private EntityQuery                             removeHeightMapQuery;
        private EntityQuery                             updateQuery;
        private NativeQueue<LineTerrainMinMaxHeightMap> minMaxDataToUpdateSynchronously;
        private NativeArray<int2x2>                     modifiedBounds;

        public struct EntityAndMinMaxData
        {
            public Entity Entity;
            public float2 MinMax;
        }

        [BurstCompile]
        public struct RemoveCurrentMinMaxDataJob : IJob
        {
            public NativeArray<int2x2>                          ModifiedBounds;
            public NativeMultiHashMap<int, EntityAndMinMaxData> EntityMinMaxData;
            public NativeHashMap<Entity, int2x2>                PreviousBounds;
            public NativeMultiHashMap<Entity, int>              PreviousMinMaxEntryIndexes;
            public NativeHashMap<int, float2>                   MinMaxData;
            public NativeList<int>                              RemovedIndexes;

            [ReadOnly]
            public NativeArray<Entity> Entities;

            public void Execute()
            {
                RemovedIndexes.Clear();
                for (int i = 0; i < Entities.Length; i++)
                    Execute(i);
            }

            public void Execute(int index)
            {
                ModifiedBounds[0] = new int2x2();
                if (!PreviousBounds.ContainsKey(Entities[index]))
                    return;
                var entity = Entities[index];
                if (!PreviousMinMaxEntryIndexes.TryGetFirstValue(entity, out int minMaxEntryIndex,
                                                                 out NativeMultiHashMapIterator<Entity> it))
                {
                    PreviousBounds.Remove(entity);
                    return;
                }

                do
                {
                    if (EntityMinMaxData.TryGetFirstValue(minMaxEntryIndex, out EntityAndMinMaxData minMaxData,
                                                          out NativeMultiHashMapIterator<int> it2))
                    {
                        // Should always be true TODO: Decide if needed
                        if (MinMaxData.ContainsKey(minMaxEntryIndex))
                        {
                            MinMaxData.Remove(minMaxEntryIndex);
                            if (!RemovedIndexes.Contains(minMaxEntryIndex))
                            {
                                RemovedIndexes.Add(minMaxEntryIndex);
                            }
                        }

                        do
                        {
                            if (minMaxData.Entity.Equals(entity))
                            {
                                EntityMinMaxData.Remove(it2);
                            }
                            else
                            {
                                if (!MinMaxData.ContainsKey(minMaxEntryIndex))
                                {
                                    MinMaxData.TryAdd(minMaxEntryIndex, minMaxData.MinMax);
                                }
                                else
                                {
                                    MinMaxData[minMaxEntryIndex] = new float2
                                                                   {
                                                                       x = math.max(minMaxData.MinMax.x,
                                                                                    MinMaxData[minMaxEntryIndex].x),
                                                                       y = math.min(minMaxData.MinMax.y,
                                                                                    MinMaxData[minMaxEntryIndex].y)
                                                                   };
                                }
                            }
                        } while (EntityMinMaxData.TryGetNextValue(out minMaxData, ref it2));

                        PreviousMinMaxEntryIndexes.Remove(it);
                    }
                } while (PreviousMinMaxEntryIndexes.TryGetNextValue(out minMaxEntryIndex, ref it));

                ModifiedBounds[0] = CombineBounds(PreviousBounds[entity], ModifiedBounds[0]);
                PreviousBounds.Remove(entity);
            }
        }

        public struct ResizeArrays : IJob
        {
            [ReadOnly]
            public NativeArray<Entity> Entities;

            [ReadOnly]
            public BufferFromEntity<LineTerrainMinMaxHeightMap> HeightMaps;

            public NativeHashMap<Entity, int2x2>                EntityMapBounds;
            public NativeMultiHashMap<int, EntityAndMinMaxData> EntityMinMaxData;
            public NativeMultiHashMap<Entity, int>              EntityMinMaxEntryIndexes;

            public void Execute()
            {
                var sizeEntityMapBounds          = EntityMapBounds.Count() + Entities.Length;
                var sizeEntityMinMaxData         = EntityMinMaxData.Count();
                var sizeEntityMinMaxEntryIndexes = EntityMinMaxEntryIndexes.Count();

                for (int i = 0; i < Entities.Length; i++)
                {
                    var hmLen = HeightMaps[Entities[i]].Length;
                    sizeEntityMinMaxData         += hmLen;
                    sizeEntityMinMaxEntryIndexes += hmLen;
                }

                // TODO Only resize in chunks, and perhaps down size if significantly smaller
                if (EntityMapBounds.Capacity < sizeEntityMapBounds)
                    EntityMapBounds.Capacity = sizeEntityMapBounds;
                if (EntityMinMaxData.Capacity < sizeEntityMinMaxData)
                    EntityMinMaxData.Capacity = sizeEntityMinMaxData;
                if (EntityMinMaxEntryIndexes.Capacity < sizeEntityMinMaxEntryIndexes)
                    EntityMinMaxEntryIndexes.Capacity = sizeEntityMinMaxEntryIndexes;
            }
        }

        [BurstCompile]
        public struct AddNewMinMaxDataJob : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<Entity> Entities;

            [ReadOnly]
            public ComponentDataFromEntity<HeightMapChange> Changes;

            [ReadOnly]
            public BufferFromEntity<LineTerrainMinMaxHeightMap> HeightMaps;

            public NativeHashMap<Entity, int2x2>.ParallelWriter                EntityMapBounds;
            public NativeMultiHashMap<int, EntityAndMinMaxData>.ParallelWriter EntityMinMaxData;
            public NativeMultiHashMap<Entity, int>.ParallelWriter              EntityMinMaxEntryIndexes;

            public void Execute(int index)
            {
                var entity = Entities[index];
                if (HeightMaps[entity].Length == 0)
                    return;
                if (!EntityMapBounds.TryAdd(entity, new int2x2(Changes[entity].StartPosition, Changes[entity].Size)))
                {
                    throw new Exception("AddNewMinMaxDataJob: Unable to add bounds to EntityMapBounds");
                }

                var hm = HeightMaps[entity];
                for (int i = 0; i < hm.Length; i++)
                {
                    var hashCode = hm[i].Position.GetHashCode();
                    EntityMinMaxData.Add(
                                         hashCode,
                                         new EntityAndMinMaxData
                                         {
                                             Entity = entity,
                                             MinMax = new float2(hm[i].Min, hm[i].Max)
                                         });
                    EntityMinMaxEntryIndexes.Add(entity, hashCode);
                }
            }
        }

        public struct UpdateModifiedBounds : IJob
        {
            public NativeArray<int2x2> ModifiedBounds;

            [ReadOnly]
            public NativeHashMap<Entity, int2x2> EntityMapBounds;

            public NativeArray<Entity> Entities;

            public void Execute()
            {
                var bounds = ModifiedBounds[0];
                for (int i = 0; i < Entities.Length; i++)
                {
                    if (EntityMapBounds.ContainsKey(Entities[i]))
                        bounds = CombineBounds(EntityMapBounds[Entities[i]], bounds);
                }

                ModifiedBounds[0] = bounds;
            }
        }


        [BurstCompile]
        public struct HeightMapAddMinMaxDataJob2 : IJobParallelFor
        {
            public NativeHashMap<int, float2>.ParallelWriter              MinMaxData;
            public NativeQueue<LineTerrainMinMaxHeightMap>.ParallelWriter MinMaxDataToUpdateSynchronously;
            public NativeQueue<int>.ParallelWriter                        IndexesModified;

            [ReadOnly]
            public NativeArray<Entity> Entities;

            [ReadOnly]
            public BufferFromEntity<LineTerrainMinMaxHeightMap> HeightMaps;

            public void Execute(int index)
            {
                var buffer = HeightMaps[Entities[index]];
                var len    = buffer.Length;
                for (int i = 0; i < len; i++)
                {
                    var hashCode = buffer[i].Position.GetHashCode();

                    if (!MinMaxData.TryAdd(hashCode,
                                           new float2(buffer[i].Min, buffer[i].Max)))
                    {
                        MinMaxDataToUpdateSynchronously.Enqueue(buffer[i]);
                    }
                    else
                    {
                        IndexesModified.Enqueue(hashCode);
                    }
                }
            }
        }


        [BurstCompile]
        public struct HeightMapUpdateMinMaxDataJob2 : IJob
        {
            public NativeHashMap<int, float2>              MinMaxData;
            public NativeQueue<LineTerrainMinMaxHeightMap> MinMaxDataToUpdateSynchronously;
            public NativeQueue<int>                        IndexesModified;

            public void Execute()
            {
                while (MinMaxDataToUpdateSynchronously.TryDequeue(out var item))
                {
                    var hashCode = item.Position.GetHashCode();
                    MinMaxData[hashCode] =
                        new float2(math.max(item.Min, MinMaxData[hashCode].x),
                                   math.min(item.Max, MinMaxData[hashCode].y));
                    IndexesModified.Enqueue(hashCode);
                }
            }
        }

        public struct UpdateModifiedOrRemovedIndexes : IJob
        {
            public NativeQueue<int> IndexesModified;
            public NativeList<int>  Removed;
            public NativeList<int>  Modified;

            public void Execute()
            {
                Modified.Clear();
                while (IndexesModified.TryDequeue(out int index))
                {
                    if (Removed.Contains(index))
                    {
                        Removed.RemoveAt(Removed.IndexOf(index));
                    }

                    Modified.Add(index);
                }

                IndexesModified.Clear();
            }
        }

        [BurstCompile]
        public struct UpdateFilteredData2 : IJobParallelForDefer
        {
            [ReadOnly]
            public NativeHashMap<int, float2> MinMaxData;


            [ReadOnly]
            public NativeHashMap<int, float> ActualHeightData;

            [NativeDisableParallelForRestriction]
            public NativeHashMap<int, float> FilteredHeightData;

            [ReadOnly]
            public NativeList<int> ModifiedIndexes;

            public void Execute(int index)
            {
                var a = ActualHeightData[ModifiedIndexes[index]];
                var b = MinMaxData[ModifiedIndexes[index]];
                //var x = FilteredHeightData[ModifiedIndexes[index]];
                var x = math.clamp(a, b.x, b.y);
                FilteredHeightData[ModifiedIndexes[index]] = x;
            }
        }

        public struct UpdateFilteredData3 : IJobParallelForDefer
        {
            [ReadOnly]
            public NativeHashMap<int, float> ActualHeightData;

            [NativeDisableParallelForRestriction]
            public NativeHashMap<int, float> FilteredHeightData;

            [ReadOnly]
            public NativeList<int> RemovedIndexes;

            public void Execute(int index)
            {
                FilteredHeightData[RemovedIndexes[index]] = ActualHeightData[RemovedIndexes[index]];
            }
        }

        [BurstCompile]
        public struct TriggerUpdateTerrainHeightMapJob2 : IJob
        {
            [ReadOnly]
            public NativeArray<int2x2> ModifiedBounds;
            //[ReadOnly]
            //public

            public EntityCommandBuffer Ecb;

            public void Execute()
            {
                var ent = Ecb.CreateEntity();
                Ecb.AddComponent(ent, new HeightMapUpdateTrigger
                                      {
                                          StartPosition = ModifiedBounds[0].c0,
                                          Size          = ModifiedBounds[0].c1
                                      });
                //var buff = Ecb.SetBuffer<ModifiedHeightMapIndex>(ent);
            }
        }

        public static int2x2 CombineBounds(int2x2 a, int2x2 b)
        {
            var result = new int2x2();
            if (a.c1.Equals(int2.zero))
                return b;
            if (b.c1.Equals(int2.zero))
                return a;
            // Increase Bounds if applicable
            var endIndexX = math.max(a.c0.x + a.c1.x,
                                     b.c0.x + b.c1.x);
            var endIndexY = math.max(a.c0.y + a.c1.y,
                                     b.c0.y + b.c1.y);
            result.c0.x = math.min(a.c0.x, b.c0.x);
            result.c0.y = math.min(a.c0.y, b.c0.y);
            result.c1.x = endIndexX - b.c0.x;
            result.c1.y = endIndexY - b.c0.y;
            return result;
        }

        protected override void OnUpdate()
        {
            Dispose();
            var entities                = heightMapChangeQuery.ToEntityArrayAsync(Allocator.TempJob, out var jh1);


            var removeEntities = removeHeightMapQuery.ToEntityArrayAsync(Allocator.TempJob, out JobHandle jh2);
            JobHandle.CombineDependencies(jh1,jh2).Complete();
            var removeAndChangeEntities = new NativeArray<Entity>(removeEntities.Length + entities.Length, Allocator.TempJob);
            for (int i = 0; i < entities.Length; i++)
            {
                removeAndChangeEntities[i] = entities[i];
            }

            for (int i = entities.Length; i < entities.Length + removeEntities.Length; i++)
            {
                removeAndChangeEntities[i] = EntityManager.GetComponentData<RemoveHeightMap>(removeEntities[i]).HeightMapOwner;
            }

            removeEntities.Dispose();

            var count                   = entities.Length;
            minMaxDataToUpdateSynchronously = new NativeQueue<LineTerrainMinMaxHeightMap>(Allocator.TempJob);

            var roHeightMapsFromEntity = GetBufferFromEntity<LineTerrainMinMaxHeightMap>(true);
            var roChangesFromEntity    = GetComponentDataFromEntity<HeightMapChange>(true);

            //Dependency = JobHandle.CombineDependencies(World.GetExistingSystem<HeightMapRemoveEntityDataSystem>().Dep, Dependency);

            Dependency = new RemoveCurrentMinMaxDataJob
                         {
                             Entities                   = removeAndChangeEntities,
                             ModifiedBounds             = modifiedBounds,
                             EntityMinMaxData           = EntityMinMaxData,
                             PreviousBounds             = EntityMapBounds,
                             PreviousMinMaxEntryIndexes = EntityMinMaxEntryIndexes,
                             MinMaxData                 = MinMaxData,
                             RemovedIndexes             = removedIndexes
                         }.Schedule(JobHandle.CombineDependencies(jh1, jh2, Dependency));

            Dependency = new ResizeArrays
                         {
                             Entities                 = entities,
                             HeightMaps               = roHeightMapsFromEntity,
                             EntityMapBounds          = EntityMapBounds,
                             EntityMinMaxData         = EntityMinMaxData,
                             EntityMinMaxEntryIndexes = EntityMinMaxEntryIndexes
                         }.Schedule(Dependency);

            Dependency = new AddNewMinMaxDataJob
                         {
                             Entities                 = entities,
                             Changes                  = roChangesFromEntity,
                             HeightMaps               = roHeightMapsFromEntity,
                             EntityMapBounds          = EntityMapBounds.AsParallelWriter(),
                             EntityMinMaxData         = EntityMinMaxData.AsParallelWriter(),
                             EntityMinMaxEntryIndexes = EntityMinMaxEntryIndexes.AsParallelWriter()
                         }.Schedule(count, 4, Dependency);

            Dependency = new UpdateModifiedBounds
                         {
                             Entities        = entities,
                             ModifiedBounds  = modifiedBounds,
                             EntityMapBounds = EntityMapBounds
                         }.Schedule(Dependency);

            Dependency = new HeightMapAddMinMaxDataJob2
                         {
                             Entities                        = entities,
                             HeightMaps                      = roHeightMapsFromEntity,
                             MinMaxData                      = MinMaxData.AsParallelWriter(),
                             MinMaxDataToUpdateSynchronously = minMaxDataToUpdateSynchronously.AsParallelWriter(),
                             IndexesModified                 = IndexesModifiedQueue.AsParallelWriter()
                         }.Schedule(count, 4, Dependency);

            Dependency = new HeightMapUpdateMinMaxDataJob2
                         {
                             MinMaxData                      = MinMaxData,
                             MinMaxDataToUpdateSynchronously = minMaxDataToUpdateSynchronously,
                             IndexesModified                 = IndexesModifiedQueue
                         }.Schedule(Dependency);

            var hJh2 = new UpdateModifiedOrRemovedIndexes
                       {
                           Modified        = modifiedIndexes,
                           Removed         = removedIndexes,
                           IndexesModified = IndexesModifiedQueue
                       }.Schedule(Dependency);

            var uJh1 = new UpdateFilteredData2
                       {
                           ModifiedIndexes    = modifiedIndexes,
                           MinMaxData         = MinMaxData,
                           ActualHeightData   = ActualHeightData,
                           FilteredHeightData = FilteredHeightData,
                       }.Schedule(modifiedIndexes, 32, hJh2);

            var uJh2 = new UpdateFilteredData3
                       {
                           RemovedIndexes     = removedIndexes,
                           ActualHeightData   = ActualHeightData,
                           FilteredHeightData = FilteredHeightData,
                       }.Schedule(removedIndexes, 32, uJh1);

            var tJh1 = new TriggerUpdateTerrainHeightMapJob2
                       {
                           Ecb            = LineEndSimBufferSystem.Instance.CreateCommandBuffer(),
                           ModifiedBounds = modifiedBounds
                       }.Schedule(Dependency);

            Dependency = new DeallocateJob<Entity, Entity>
                         {
                             NativeArray1 = entities,
                             NativeArray2 = removeAndChangeEntities
                         }.Schedule(JobHandle.CombineDependencies(JobHandle.CombineDependencies(uJh2, tJh1),
                                                                  Dependency));

            LineEndSimBufferSystem.Instance.AddJobHandleForProducer(Dependency);

            var ecb = LineEndSimBufferSystem.Instance.CreateCommandBuffer();
            ecb
               .RemoveComponent<HeightMapChange>(heightMapChangeQuery);
            ecb.DestroyEntity(removeHeightMapQuery);
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
            MinMaxData.Dispose();
            ActualHeightData.Dispose();
            FilteredHeightData.Dispose();
            modifiedBounds.Dispose();
            EntityMapBounds.Dispose();
            EntityMinMaxData.Dispose();
            EntityMinMaxEntryIndexes.Dispose();
            removedIndexes.Dispose();
            modifiedIndexes.Dispose();
            IndexesModifiedQueue.Dispose();
            actualHeightMapData.Dispose();
            filteredHeightMapData.Dispose();
        }

        public void FirstRun()
        {
            var td      = Terrain.activeTerrain.terrainData;
            var heights = td.GetHeights(0, 0, td.heightmapResolution, td.heightmapResolution);
            ActualHeightData = new NativeHashMap<int, float>(heights.GetLength(0) * heights.GetLength(1),
                                                             Allocator.Persistent);
            MinMaxData = new NativeHashMap<int, float2>(heights.GetLength(0) * heights.GetLength(1),
                                                        Allocator.Persistent);
            FilteredHeightData = new NativeHashMap<int, float>(heights.GetLength(0) * heights.GetLength(1),
                                                               Allocator.Persistent);
            modifiedBounds           = new NativeArray<int2x2>(1, Allocator.Persistent);
            EntityMapBounds          = new NativeHashMap<Entity, int2x2>(1, Allocator.Persistent);
            EntityMinMaxData         = new NativeMultiHashMap<int, EntityAndMinMaxData>(1, Allocator.Persistent);
            EntityMinMaxEntryIndexes = new NativeMultiHashMap<Entity, int>(1, Allocator.Persistent);
            modifiedIndexes          = new NativeList<int>(heights.GetLength(0), Allocator.Persistent);
            removedIndexes           = new NativeList<int>(heights.GetLength(0), Allocator.Persistent);
            IndexesModifiedQueue     = new NativeQueue<int>(Allocator.Persistent);
            actualHeightMapData =
                new NativeArray<float>(heights.GetLength(0) * heights.GetLength(1), Allocator.Persistent);
            filteredHeightMapData =
                new NativeArray<float>(heights.GetLength(0) * heights.GetLength(1), Allocator.Persistent);

            for (int x = 0; x < heights.GetLength(1); x++)
            for (int y = 0; y < heights.GetLength(0); y++)
            {
                actualHeightMapData[FlattenVector(x, y)]   = heights[y, x];
                filteredHeightMapData[FlattenVector(x, y)] = heights[y, x];
                ActualHeightData.Add(new int2(x, y).GetHashCode(), heights[y, x]);
                FilteredHeightData.Add(new int2(x, y).GetHashCode(), heights[y, x]);
            }
        }

        public static int FlattenVector(int2 vector, int ySize) => FlattenVector(vector.x, vector.y, ySize);

        public static int FlattenVector(int x, int y, int ySize)
        {
            return x * ySize + y;
        }

        public static int2 UnFlattenVector(int x, int y, int ySize)
        {
            return new int2((int) math.floor(x / (float) ySize), y % ySize);
        }

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
            removeHeightMapQuery = GetEntityQuery(typeof(RemoveHeightMap));
            updateQuery = GetEntityQuery(new EntityQueryDesc
                                         {
                                             Any = new[]
                                                   {
                                                       ComponentType.ReadOnly<HeightMapChange>(),
                                                       ComponentType.ReadOnly<RemoveHeightMap>(),
                                                   }
                                         });
            RequireForUpdate(updateQuery);
        }
    }
}