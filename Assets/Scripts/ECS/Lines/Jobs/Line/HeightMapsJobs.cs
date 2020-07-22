using Sibz.Lines.ECS.Components;
using Sibz.Lines.ECS.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Sibz.Lines.ECS.Jobs
{
    public struct HeightMapsJobs
    {
        [ReadOnly]
        public NativeArray<Entity> Entities;

        [ReadOnly]
        public ComponentDataFromEntity<Line> Lines;

        [ReadOnly]
        public ComponentDataFromEntity<LineProfile> LineProfilesFromEntity;

        [ReadOnly]
        public BufferFromEntity<LineKnotData> KnotData;

        [NativeDisableParallelForRestriction]
        public BufferFromEntity<LineTerrainMinMaxHeightMap> HeightMapBuffers;

        [ReadOnly]
        public NativeArray<float3x2> BoundsArray;

        public int    HeightMapResolution;
        public float3 TerrainSize;


        private NativeList<LineKnotData> combinedKnotData;
        private NativeList<float2x2>     heightData;
        private NativeList<bool>         heightSet;
        private NativeList<int>          entityIndex;

        public void Dispose()
        {
            if (combinedKnotData.IsCreated)
                combinedKnotData.Dispose();
            if (heightData.IsCreated)
                heightData.Dispose();
            if (heightSet.IsCreated)
                heightSet.Dispose();
            if (entityIndex.IsCreated)
                entityIndex.Dispose();
        }

        public JobHandle Schedule(JobHandle dependency)
        {
            //Dispose();
            var e = new NativeList<Entity>(Allocator.TempJob);
            for (var i = 0; i < Entities.Length; i++)
                if (!e.Contains(Entities[i]))
                    e.Add(Entities[i]);

            var count                      = e.Length;
            var entities                   = new NativeArray<Entity>(e, Allocator.TempJob);
            var lineProfiles               = new NativeArray<LineProfile>(count, Allocator.TempJob);
            var maxDistances               = new NativeArray<float4>(count, Allocator.TempJob);
            var extended2DBoundsArray      = new NativeArray<int2x2>(count, Allocator.TempJob);
            var knotOffsetAndLengths       = new NativeArray<int2>(count, Allocator.TempJob);
            var heightDataOffsetAndLengths = new NativeArray<int2>(count, Allocator.TempJob);
            combinedKnotData = new NativeList<LineKnotData>(Allocator.TempJob);
            heightData       = new NativeList<float2x2>(Allocator.TempJob);
            heightSet        = new NativeList<bool>(Allocator.TempJob);
            entityIndex      = new NativeList<int>(Allocator.TempJob);
            e.Dispose();

            var dependency2 = new CombineKnotData
                              {
                                  Entities             = entities,
                                  KnotData             = KnotData,
                                  CombinedKnotData     = combinedKnotData,
                                  KnotOffsetAndLengths = knotOffsetAndLengths
                              }.Schedule(dependency);

            dependency = new GetProfiles
                         {
                             Entities               = entities,
                             Lines                  = Lines,
                             LineProfilesFromEntity = LineProfilesFromEntity,
                             LineProfiles           = lineProfiles,
                             DefaultProfile         = LineProfile.Default()
                         }.Schedule(count, 4, dependency);

            dependency = new GetMaxDistances
                         {
                             LineProfiles = lineProfiles,
                             MaxDistances = maxDistances
                         }.Schedule(count, 4, dependency);

            dependency = new GetBoundsJob
                         {
                             BoundsArray           = BoundsArray,
                             MaxDistances          = maxDistances,
                             Extended2DBoundsArray = extended2DBoundsArray,
                             HeightMapResolution   = HeightMapResolution,
                             TerrainSize           = TerrainSize
                         }.Schedule(count, 4, dependency);

            dependency = new GetCombinedHeightDataList
                         {
                             Entities                   = entities,
                             Extended2DBoundsArray      = extended2DBoundsArray,
                             HeightDataOffsetAndLengths = heightDataOffsetAndLengths,
                             HeightData                 = heightData,
                             HeightSet                  = heightSet,
                             EntityIndex                = entityIndex
                         }.Schedule(dependency);


            dependency = new UpdateHeightData
                         {
                             EntityIndex                = entityIndex,
                             KnotOffsetAndLengths       = knotOffsetAndLengths,
                             CombinedKnotData           = combinedKnotData,
                             HeightDataOffsetAndLengths = heightDataOffsetAndLengths,
                             Extended2DBoundsArray      = extended2DBoundsArray,
                             LineProfiles               = lineProfiles,
                             MaxDistances               = maxDistances,
                             HeightData                 = heightData,
                             HeightSet                  = heightSet,
                             TerrainSize                = TerrainSize,
                             HeightMapResolution        = HeightMapResolution
                         }.Schedule(heightData, 8, JobHandle.CombineDependencies(dependency, dependency2));

            dependency = new UpdateEntityHeightMapData
                         {
                             Ecb = LineEndSimBufferSystem.Instance.CreateCommandBuffer().ToConcurrent(),
                             Entities = entities,
                             HeightData = heightData,
                             HeightSet = heightSet,
                             HeightMapBuffers = HeightMapBuffers,
                             HeightDataOffsetAndLengths = heightDataOffsetAndLengths
                         }.Schedule(count, 1, dependency);

            dependency = new DeallocateJob<Entity, LineProfile, float4, int2x2>
                         {
                             NativeArray1 = entities,
                             NativeArray2 = lineProfiles,
                             NativeArray3 = maxDistances,
                             NativeArray4 = extended2DBoundsArray
                         }.Schedule(dependency);
            dependency = new DeallocateJob<int2, int2 /*, LineKnotData, float2x2*/>
                         {
                             NativeArray1 = knotOffsetAndLengths,
                             NativeArray2 = heightDataOffsetAndLengths,
                             /*NativeArray3 = combinedKnotData.AsDeferredJobArray(),
                             NativeArray4 = heightData.AsDeferredJobArray()*/
                         }.Schedule(dependency);
            /*dependency = new DeallocateJob<bool, int>
                         {
                             NativeArray1 = heightSet.AsDeferredJobArray(),
                             NativeArray2 = entityIndex.AsDeferredJobArray()
                         }.Schedule(dependency);*/

            LineEndSimBufferSystem.Instance.AddJobHandleForProducer(dependency);

            return dependency;
        }

        [BurstCompile]
        public struct GetProfiles : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<Entity> Entities;

            [ReadOnly]
            public ComponentDataFromEntity<Line> Lines;

            [ReadOnly]
            public ComponentDataFromEntity<LineProfile> LineProfilesFromEntity;

            [NativeDisableParallelForRestriction]
            public NativeArray<LineProfile> LineProfiles;

            [ReadOnly]
            public LineProfile DefaultProfile;

            public void Execute(int index)
            {
                LineProfiles[index] = LineProfilesFromEntity.Exists(Lines[Entities[index]].Profile)
                                          ? LineProfilesFromEntity[Lines[Entities[index]].Profile]
                                          : DefaultProfile;
            }
        }

        [BurstCompile]
        public struct GetMaxDistances : IJobParallelFor
        {
            [NativeDisableParallelForRestriction]
            public NativeArray<float4> MaxDistances;

            [ReadOnly]
            public NativeArray<LineProfile> LineProfiles;

            public void Execute(int index)
            {
                var c = LineProfiles[index].TerrainConstraints;
                MaxDistances[index] = new float4
                                      {
                                          x = GetOpposite(c.EndCuttingAngle, c.MaxDepth),
                                          y = GetOpposite(c.EndRiseAngle, c.MaxRise),
                                          z = GetOpposite(c.SideCuttingAngle, c.MaxDepth),
                                          w = GetOpposite(c.SideRiseAngle, c.MaxRise)
                                      };
            }

            private static float GetOpposite(float angle, float maxDist)
            {
                return math.tan(0.5f * math.PI - angle) * maxDist;
            }
        }

        [BurstCompile]
        public struct GetBoundsJob : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<float4> MaxDistances;

            [ReadOnly]
            public NativeArray<float3x2> BoundsArray;

            [NativeDisableParallelForRestriction]
            public NativeArray<int2x2> Extended2DBoundsArray;

            public float  HeightMapResolution;
            public float3 TerrainSize;

            public void Execute(int index)
            {
                var max = math.max(MaxDistances[index].x,
                                   math.max(MaxDistances[index].y,
                                            math.max(MaxDistances[index].z,
                                                     MaxDistances[index].w)));
                var bounds = BoundsArray[index];
                Extended2DBoundsArray[index] =
                    new int2x2
                    {
                        c0 =
                        {
                            x = (int) ((-max + bounds.c0.x - bounds.c1.x / 2) / TerrainSize.x * HeightMapResolution),
                            y = (int) ((-max + bounds.c0.z - bounds.c1.z / 2) / TerrainSize.z * HeightMapResolution)
                        },
                        c1 =
                        {
                            x = (int) ((bounds.c1.x + max * 2) / TerrainSize.x * HeightMapResolution),
                            y = (int) ((bounds.c1.z + max * 2) / TerrainSize.z * HeightMapResolution)
                        }
                    };
            }
        }

        [BurstCompile]
        public struct CombineKnotData : IJob
        {
            [ReadOnly]
            public NativeArray<Entity> Entities;

            [ReadOnly]
            public BufferFromEntity<LineKnotData> KnotData;

            public NativeArray<int2>        KnotOffsetAndLengths;
            public NativeList<LineKnotData> CombinedKnotData;

            public void Execute()
            {
                for (var i = 0; i < Entities.Length; i++)
                {
                    var kd    = KnotData[Entities[i]];
                    var kdLen = kd.Length;
                    KnotOffsetAndLengths[i] = new int2(CombinedKnotData.Length, kdLen);
                    for (var j = 0; j < kdLen; j++) CombinedKnotData.Add(kd[j]);
                }
            }
        }

        [BurstCompile]
        public struct GetCombinedHeightDataList : IJob
        {
            [ReadOnly]
            public NativeArray<Entity> Entities;

            [ReadOnly]
            public NativeArray<int2x2> Extended2DBoundsArray;

            public NativeArray<int2>    HeightDataOffsetAndLengths;
            public NativeList<float2x2> HeightData;
            public NativeList<bool>     HeightSet;
            public NativeList<int>      EntityIndex;

            public void Execute()
            {
                for (var i = 0; i < Entities.Length; i++)
                {
                    var boundsSize = Extended2DBoundsArray[i].c1;
                    var boundsLen  = boundsSize.x * boundsSize.y;
                    HeightDataOffsetAndLengths[i] = new int2(HeightData.Length, boundsLen);
                    HeightData.Resize(HeightData.Length + boundsLen, NativeArrayOptions.UninitializedMemory);
                    HeightSet.Resize(HeightSet.Length + boundsLen, NativeArrayOptions.ClearMemory);
                    for (var j = 0; j < boundsLen; j++) EntityIndex.Add(i);
                }
            }
        }

        public struct UpdateHeightData : IJobParallelForDefer
        {
            [ReadOnly]
            public NativeList<int> EntityIndex;

            [ReadOnly]
            public NativeArray<int2> KnotOffsetAndLengths;

            [ReadOnly]
            public NativeList<LineKnotData> CombinedKnotData;

            [ReadOnly]
            public NativeArray<int2> HeightDataOffsetAndLengths;

            [ReadOnly]
            public NativeArray<int2x2> Extended2DBoundsArray;

            [ReadOnly]
            public NativeArray<LineProfile> LineProfiles;

            [ReadOnly]
            public NativeArray<float4> MaxDistances;

            [NativeDisableParallelForRestriction]
            public NativeList<float2x2> HeightData;

            [NativeDisableParallelForRestriction]
            public NativeList<bool> HeightSet;

            public int    HeightMapResolution;
            public float3 TerrainSize;

            private int          entityIndex;
            private int          index;
            private int2         heightMapPosition;
            private float3       worldPosition;
            private LineKnotData closestKnot;
            private int          closestKnotIndex;
            private LineProfile  lineProfile;

            public void Execute(int i)
            {
                index       = i;
                entityIndex = EntityIndex[i];
                lineProfile = LineProfiles[entityIndex];
                var knotData = new NativeSlice<LineKnotData>(CombinedKnotData,
                                                             KnotOffsetAndLengths[entityIndex].x,
                                                             KnotOffsetAndLengths[entityIndex].y);

                if (knotData.Length == 0)
                    return;

                SetHeightMapPosition();
                ToWorldPos(heightMapPosition, out worldPosition);
                SetClosesKnot(knotData);
                SetMinMax();
            }

            public void SetMinMax()
            {
                var closestPosWithoutHeight = closestKnot.Position;
                closestPosWithoutHeight.y = 0;

                var dist              = math.distance(closestPosWithoutHeight, worldPosition);

                var closestKnotHeight = closestKnot.Position.y / TerrainSize.y;
                if (dist < lineProfile.Width / 1.5)
                {
                    HeightData[index] =
                        new float2x2(heightMapPosition, new float2(closestKnotHeight, closestKnotHeight));
                    HeightSet[index] = true;
                    return;
                }

                if (dist > MaxDistances[entityIndex].z)
                {
                    HeightSet[index] = false;
                    return;
                }
                /*HeightSet[index] = false;
                return;*/
                var distFromEdge = dist - lineProfile.Width /2;

                float GetCurveVector(float maxDist, float maxChange, LineProfile profile)
                {
                    var t = distFromEdge / maxDist;
                    return math.lerp(closestKnotHeight, closestKnotHeight + maxChange, t);
                    /*
                    var pointA = new float2(0, closestKnotHeight);
                    var controlPoint = new float2(0.01f,
                                                  closestKnotHeight);
                    var pointB = new float2(1, maxChange);
                    return Bezier.Bezier.GetVectorOnCurve(pointA, controlPoint, pointB, t).y;*/
                }

                var max = GetCurveVector(MaxDistances[entityIndex].z,
                                         lineProfile.TerrainConstraints.MaxRise / TerrainSize.y, lineProfile);
                var min = GetCurveVector(MaxDistances[entityIndex].z,
                                         -(lineProfile.TerrainConstraints.MaxDepth / TerrainSize.y), lineProfile);

                HeightData[index] = new float2x2(heightMapPosition, new float2(min, max));
                HeightSet[index]  = true;
            }

            private void SetClosesKnot(NativeSlice<LineKnotData> knotData)
            {
                TryGetClosestKnot(knotData, 0, knotData.Length - 1, out closestKnotIndex);
                closestKnot = knotData[closestKnotIndex];
            }

            private void TryGetClosestKnot(NativeSlice<LineKnotData> knotData, int start, int end, out int closestIndex)
            {
                while (true)
                {
                    if (start == end)
                    {
                        closestIndex = start;
                        return;
                    }

                    if (start + 1 == end)
                    {
                        closestIndex = math.distance(knotData[start].Position, worldPosition) >
                                       math.distance(knotData[end].Position, worldPosition)
                                           ? end
                                           : start;
                        return;
                    }

                    var centre = start + (end - start) / 2;
                    var distA  = math.distance(knotData[start].Position, worldPosition);
                    //var distB  = math.distance(knotData[centre].Position, worldPosition);
                    var distC = math.distance(knotData[end].Position, worldPosition);
                    start = distA < distC ? start : centre;
                    end   = distA < distC ? centre : end;
                }
            }

            public void SetHeightMapPosition()
            {
                var offsetAndLength = HeightDataOffsetAndLengths[entityIndex];
                var bounds          = Extended2DBoundsArray[entityIndex];
                var relativeIndex   = index - offsetAndLength.x;
                var x               = (int) math.floor(relativeIndex / (float) bounds.c1.y);
                var y               = relativeIndex % bounds.c1.y;
                heightMapPosition = new int2(x + bounds.c0.x, y + bounds.c0.y);
            }

            private void ToWorldPos(int2 heightMapPos, out float3 worldPos)
            {
                worldPos =
                    new float3
                    {
                        x = (heightMapPos.x + 0.5f) / (float) HeightMapResolution * TerrainSize.x,
                        z = (heightMapPos.y + 0.5f) / (float) HeightMapResolution * TerrainSize.z
                    };
            }
        }

        [BurstCompile]
        public struct UpdateEntityHeightMapData : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<Entity> Entities;

            [ReadOnly]
            public NativeArray<int2> HeightDataOffsetAndLengths;

            [ReadOnly]
            public NativeList<float2x2> HeightData;

            [ReadOnly]
            public NativeList<bool> HeightSet;

            [NativeDisableParallelForRestriction]
            public BufferFromEntity<LineTerrainMinMaxHeightMap> HeightMapBuffers;

            public EntityCommandBuffer.Concurrent Ecb;

            public void Execute(int index)
            {
                var heightData = new NativeSlice<float2x2>(HeightData,
                                                           HeightDataOffsetAndLengths[index].x,
                                                           HeightDataOffsetAndLengths[index].y);
                var heightSetData = new NativeSlice<bool>(HeightSet,
                                                          HeightDataOffsetAndLengths[index].x,
                                                          HeightDataOffsetAndLengths[index].y);
                var heightMapBuffer = HeightMapBuffers[Entities[index]];
                var len             = heightData.Length;
                var lowest          = int2.zero;
                var highest         = int2.zero;

                if (len == 0) return;

                for (var i = 0; i < len; i++)
                {
                    if (!heightSetData[i])
                        continue;

                    if (i == 0)
                    {
                        lowest  = new int2(heightData[i].c0);
                        highest = lowest;
                    }

                    var position = new int2(heightData[i].c0);

                    heightMapBuffer.Add(new LineTerrainMinMaxHeightMap
                                        {
                                            Position = position,
                                            Min      = heightData[i].c1.x,
                                            Max      = heightData[i].c1.y
                                        });

                    lowest.x  = math.min(position.x, lowest.x);
                    lowest.y  = math.min(position.y, lowest.y);
                    highest.x = math.max(position.x, highest.x);
                    highest.y = math.max(position.y, highest.y);
                }

                Ecb.AddComponent(index, Entities[index], new HeightMapChange
                                                         {
                                                             StartPosition = lowest,
                                                             Size          = highest - lowest
                                                         });
            }
        }
    }
}