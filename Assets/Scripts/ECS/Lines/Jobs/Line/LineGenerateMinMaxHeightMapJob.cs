﻿using Sibz.Lines.ECS.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

namespace Sibz.Lines.ECS.Jobs
{
    [BurstCompile]
    public struct LineGenerateMinMaxHeightMapJob2 : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<Entity> LineEntities;

        [ReadOnly]
        public ComponentDataFromEntity<LineProfile> LineProfiles;

        [ReadOnly]
        public ComponentDataFromEntity<Line> Lines;

        [ReadOnly]
        public BufferFromEntity<LineKnotData> KnotData;

        [ReadOnly]
        public NativeArray<float3x2> BoundsArray;

        [NativeDisableParallelForRestriction]
        public BufferFromEntity<LineTerrainMinMaxHeightMap> HeightMaps;

        public LineProfile                    DefaultProfile;
        public float3                         TerrainSize2;
        public int                            HeightMapResolution;
        public EntityCommandBuffer.ParallelWriter Ecb;

        private Line        line;
        private LineProfile lineProfile;
        private float3x2    bounds;

        [ReadOnly]
        private DynamicBuffer<LineKnotData> knotData;

        [ReadOnly]
        private DynamicBuffer<LineTerrainMinMaxHeightMap> heightMap;

        public void Execute(int index)
        {
            if (index != LineEntities.IndexOf<Entity>(LineEntities[index]))
                return;
            line = Lines[LineEntities[index]];
            lineProfile = LineProfiles.HasComponent(line.Profile)
                              ? LineProfiles[line.Profile]
                              : DefaultProfile;
            knotData = KnotData[LineEntities[index]];

            bounds = BoundsArray[index];
            heightMap =
                HeightMaps
                    [LineEntities[index]]; //Ecb.SetBuffer<LineTerrainMinMaxHeightMap>(index, LineEntities[index]);
            heightMap.Clear();
            if (knotData.Length < 2)
                return;

            ToHeightMapPos(knotData[0].Position, out int2 lowest);
            int2                 highest         = lowest;
            NativeList<float2x3> processedPoints = new NativeList<float2x3>(Allocator.Temp);
            NativeList<int>      addedHashes     = new NativeList<int>(Allocator.Temp);
            for (int i = 1; i < knotData.Length; i++)
            {
                var lastKnot = knotData[i - 1];
                ToHeightMapPos(lastKnot.Position, out int2 lastHMapPos);
                ToHeightMapPos(knotData[i].Position, out int2 hMapPos);
                var dist = math.distance(lastHMapPos, hMapPos);
                for (int j = 0; j < dist; j++)
                {
                    var lerped = math.lerp(lastHMapPos, hMapPos, j / dist);
                    var pointToAdd = new float2x3
                                     {
                                         c0 =
                                         {
                                             x = (int) lerped.x,
                                             y = (int) lerped.y,
                                         },
                                         c1 =
                                         {
                                             x = knotData[i].Position.y / TerrainSize2.y,
                                             y = knotData[i].Position.y / TerrainSize2.y
                                         },
                                         c2 =
                                         {
                                             x = knotData[i].Position.x,
                                             y = knotData[i].Position.z
                                         }
                                     };
                    if (addedHashes.Contains(((int2) pointToAdd.c0).GetHashCode()))
                        continue;
                    processedPoints.Add(pointToAdd);
                    addedHashes.Add(((int2) pointToAdd.c0).GetHashCode());
                    lowest.x  = math.min((int) pointToAdd.c0.x, lowest.x);
                    lowest.y  = math.min((int) pointToAdd.c0.y, lowest.y);
                    highest.x = math.max((int) pointToAdd.c0.x, highest.x);
                    highest.y = math.max((int) pointToAdd.c0.y, highest.y);

                    heightMap.Add(new LineTerrainMinMaxHeightMap
                                  {
                                      Position = new int2(pointToAdd.c0),
                                      Min      = pointToAdd.c1.x,
                                      Max      = pointToAdd.c1.y
                                  });
                }
            }

            var startIndex = 0;
            for (int i = 0; i < 5; i++)
            {
                DoLoop(heightMap, ref startIndex);
            }

            void DoLoop(DynamicBuffer<LineTerrainMinMaxHeightMap> heightMap, ref int startIndex)
            {
                var len   = processedPoints.Length;
                int start = startIndex;
                startIndex = addedHashes.Length;
                for (int i = start; i < len; i++)
                {
                    var s1 = new float2x3
                             {
                                 c0 = {x = (int) processedPoints[i].c0.x + 1, y = (int) processedPoints[i].c0.y},
                                 c1 = processedPoints[i].c1,
                                 c2 = processedPoints[i].c2
                             };
                    var s2 = new float2x3
                             {
                                 c0 = {x = (int) processedPoints[i].c0.x - 1, y = (int) processedPoints[i].c0.y},
                                 c1 = processedPoints[i].c1,
                                 c2 = processedPoints[i].c2
                             };
                    var s3 = new float2x3
                             {
                                 c0 = {x = (int) processedPoints[i].c0.x, y = (int) processedPoints[i].c0.y + 1},
                                 c1 = processedPoints[i].c1,
                                 c2 = processedPoints[i].c2
                             };
                    var s4 = new float2x3
                             {
                                 c0 = {x = (int) processedPoints[i].c0.x, y = (int) processedPoints[i].c0.y - 1},
                                 c1 = processedPoints[i].c1,
                                 c2 = processedPoints[i].c2
                             };
                    if (!addedHashes.Contains(((int2) s1.c0).GetHashCode()))
                    {
                        processedPoints.Add(s1);
                        addedHashes.Add(((int2) s1.c0).GetHashCode());
                        heightMap.Add(new LineTerrainMinMaxHeightMap
                                      {
                                          Position = new int2(s1.c0),
                                          Min      = s1.c1.x,
                                          Max      = s1.c1.y
                                      });
                    }

                    if (!addedHashes.Contains(((int2) s2.c0).GetHashCode()))
                    {
                        processedPoints.Add(s2);
                        addedHashes.Add(((int2) s2.c0).GetHashCode());
                        heightMap.Add(new LineTerrainMinMaxHeightMap
                                      {
                                          Position = new int2(s2.c0),
                                          Min      = s2.c1.x,
                                          Max      = s2.c1.y
                                      });
                    }

                    if (!addedHashes.Contains(((int2) s3.c0).GetHashCode()))
                    {
                        processedPoints.Add(s3);
                        addedHashes.Add(((int2) s3.c0).GetHashCode());
                        heightMap.Add(new LineTerrainMinMaxHeightMap
                                      {
                                          Position = new int2(s3.c0),
                                          Min      = s3.c1.x,
                                          Max      = s3.c1.y
                                      });
                    }

                    if (!addedHashes.Contains(((int2) s4.c0).GetHashCode()))
                    {
                        processedPoints.Add(s4);
                        addedHashes.Add(((int2) s4.c0).GetHashCode());
                        heightMap.Add(new LineTerrainMinMaxHeightMap
                                      {
                                          Position = new int2(s4.c0),
                                          Min      = s4.c1.x,
                                          Max      = s4.c1.y
                                      });
                    }
                }
            }

            Ecb.AddComponent(index, LineEntities[index], new HeightMapChange
                                                         {
                                                             Size          = highest - lowest,
                                                             StartPosition = lowest
                                                         });


            /*
            CalculateHeightMapBounds(out float4 maxDistances);
            ConvertBoundsToIntSize(out int2 size);
            GetBoundsCornerPosition(out int2 boundsPos);
            Ecb.AddComponent(index, LineEntities[index], new HeightMapChange
                                                         {
                                                             Size          = size,
                                                             StartPosition = boundsPos
                                                         });
            for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
            {
                //  TODO convert x / y to world position again then get min/max height at that point
                // then add results an array for another job to pickup and manage the min/max data
                ConvertIntPosToWorldPos(x, y, out float3 worldPos);
                var result = GetMinMaxHeightAtPoint(worldPos.x, worldPos.z, maxDistances, out float min, out float max);
                var hMapMin = min / TerrainSize2.y;
                var hMapMax = max / TerrainSize2.y;
                if (x == 0 && y == 0)
                {
                    hMapMax = 0.1f;
                    hMapMin = 0.1f;
                }

                // Not we need x/y in relation to terrain height map coords - not specific to individual terrain
                // but relative to 0,0

                // first need bounding box corner position in height map coords ^^
                // our x/y is simlpe boundsPos + x/y
                var pos = new int2(boundsPos.x + x, boundsPos.y + y);
                if (result.x || result.y)
                {
                    heightMap.Add(new LineTerrainMinMaxHeightMap
                                  {
                                      Position = pos,
                                      Min      = hMapMin,
                                      Max      = hMapMax
                                  });
                }
            }*/
        }

        private void ToWorldPos(int2 heightMapPos, out float3 worldPos)
        {
            worldPos =
                new float3
                {
                    x = heightMapPos.x / (float) HeightMapResolution * TerrainSize2.x,
                    z = heightMapPos.y / (float) HeightMapResolution * TerrainSize2.z
                };
        }

        private void ToHeightMapPos(float3 worldPos, out int2 heightMapPos)
        {
            heightMapPos = ToHeightMapPos(worldPos);
        }

        private int2 ToHeightMapPos(float3 worldPos)

        {
            return new int2
                   {
                       x = (int) math.floor(worldPos.x / TerrainSize2.x * HeightMapResolution),
                       y = (int) math.floor(worldPos.z / TerrainSize2.z * HeightMapResolution),
                   };
        }

        private void GetBoundsCornerPosition(out int2 boundsPos)
        {
            var x = (int) math.floor((bounds.c0.x - (bounds.c1.x / 2)) / TerrainSize2.x * HeightMapResolution);
            var z = (int) math.floor((bounds.c0.z - (bounds.c1.z / 2)) / TerrainSize2.z * HeightMapResolution);
            boundsPos = new int2(x, z);
        }

        private void ConvertIntPosToWorldPos(int x, int y, out float3 worldPos)
        {
            var cornerPos = bounds.c0 - (bounds.c1 / 2);
            worldPos = new float3
                       {
                           x = x / (float) HeightMapResolution * TerrainSize2.x + cornerPos.x,
                           z = y / (float) HeightMapResolution * TerrainSize2.z + cornerPos.z
                       };
        }

        private void ConvertBoundsToIntSize(out int2 int2)
        {
            int2 = new int2
                   {
                       x = (int) math.floor(bounds.c1.x / TerrainSize2.x * HeightMapResolution),
                       y = (int) math.floor(bounds.c1.z / TerrainSize2.z * HeightMapResolution)
                   };
        }

        public bool2 GetMinMaxHeightAtPoint(float x, float z, float4 maxDistances, out float min, out float max)
        {
            //Profiler.BeginSample("GetMinMaxHeightAtPoint");
            /* given any x & z coordinate we need to get the height at that location
             * This has to figure out this closest knot and use a bezier from the edge
             * the best way to do this is to figure our the closest end point
             * then get the half way point, figure out which is closest and work from there
             * this will save iterating the whole length of the spline down to 1/4 of the spline
             * Once we have a closest point, we can determine if we are in range and need a min/max value
             * if so the we use determine a t value and get point on curve
             */
            var vector = new float3(x, 0, z);
            var result = new bool2();
            min = 0;
            max = 0;
            if (knotData.Length < 2)
                return result;
            GetSearchKnotPairIndexes(vector, out var start, out var target);
            GetClosestKnot(vector, start, target, out int closestIndex);
            result.x = TryGetMaxHeightFromKnot(vector, closestIndex, maxDistances, out max);
            result.y = TryGetMinHeightFromKnot(vector, closestIndex, maxDistances, out min);
            //Profiler.EndSample();
            return result;
        }


        private bool TryGetMaxHeightFromKnot(float3 vector, int closestIndex, float4 maxDistances, out float height)
        {
            //Profiler.BeginSample("TryGetMaxHeightFromKnot");
            var p = knotData[closestIndex].Position;
            p.y = 0;
            var distFromKnot = math.distance(vector, p);
            if (distFromKnot < lineProfile.Width / 2)
            {
                height = knotData[closestIndex].Position.y;
                //Profiler.EndSample();
                return true;
            }

            if (distFromKnot > maxDistances.w)
            {
                height = 0;
                //Profiler.EndSample();
                return false;
            }

            var    distFromEdge = distFromKnot - lineProfile.Width / 2;
            var    t            = maxDistances.w / distFromEdge;
            float2 pointA       = new float2(0, knotData[closestIndex].Position.y);
            float2 controlPoint = new float2(lineProfile.Width / 2 / maxDistances.w, knotData[closestIndex].Position.y);
            float2 pointB       = new float2(1, lineProfile.TerrainConstraints.MaxRise);
            height = Bezier.Bezier.GetVectorOnCurve(pointA, controlPoint, pointB, t).y;
            //Profiler.EndSample();
            return true;
        }

        private bool TryGetMinHeightFromKnot(float3 vector, int closestIndex, float4 maxDistances, out float height)
        {
            //Profiler.BeginSample("TryGetMinHeightFromKnot");
            var p = knotData[closestIndex].Position;
            p.y = 0;
            var distFromKnot = math.distance(vector, p);
            if (distFromKnot < lineProfile.Width / 2)
            {
                height = knotData[closestIndex].Position.y;
                //Profiler.EndSample();
                return true;
            }

            if (distFromKnot > maxDistances.z)
            {
                height = 0;
                //Profiler.EndSample();
                return false;
            }

            var    distFromEdge = distFromKnot - lineProfile.Width / 2;
            var    t            = maxDistances.z / distFromEdge;
            float2 pointA       = new float2(0, knotData[closestIndex].Position.y);
            float2 controlPoint = new float2(lineProfile.Width / 2 / maxDistances.z, knotData[closestIndex].Position.y);
            float2 pointB       = new float2(1, -lineProfile.TerrainConstraints.MaxDepth);
            height = Bezier.Bezier.GetVectorOnCurve(pointA, controlPoint, pointB, t).y;
            //Profiler.EndSample();
            return true;
        }

        private void GetClosestKnot(float3 vector, int start, int target, out int closestIndex)
        {
            //Profiler.BeginSample("GetClosestKnot");
            bool invert = start > target;
            closestIndex = start;
            float currentClosestDist = float.MaxValue;
            for (int i = invert ? target : start; invert ? i >= start : i < target; i += (invert ? -1 : 1))
            {
                var p = knotData[i].Position;
                p.y = 0;
                var thisDist = math.distance(p, vector);
                if (currentClosestDist > thisDist)
                {
                    currentClosestDist = thisDist;
                    closestIndex       = i;
                }
            }

            //Profiler.EndSample();
        }

        private void GetSearchKnotPairIndexes(float3 vector, out int start, out int target)
        {
            //Profiler.BeginSample("GetSearchKnotPairIndexes");
            start  = 0;
            target = knotData.Length - 1;
            if (knotData.Length < 3)
            {
                return;
            }

            const int maxDiv = 20;
            var       curDiv = 0;
            while (math.abs(start - target) > 50 && curDiv <= maxDiv)
            {
                GetSearchKnotPairIndexes2(vector, math.min(start, target), math.max(start, target), out start,
                                          out target);
                curDiv++;
            }


            /*const int knotAIndex      = 0;
            var       knotCentreIndex = (knotData.Length - 1) / 2;
            var       knotBIndex      = knotData.Length - 1;
            var       knotA           = knotData[0].Position;
            knotA.y = 0;
            var knotCentre = knotData[knotCentreIndex].Position;
            knotCentre.y = 0;
            var knotB = knotData[knotBIndex].Position;
            knotB.y = 0;
            var distA      = math.distance(vector, knotA);
            var distCentre = math.distance(vector, knotCentre);
            var distB      = math.distance(vector, knotB);
            start = distA < distCentre
                        ? knotAIndex
                        : distB < distCentre
                            ? knotBIndex
                            : knotCentreIndex;
            target = distA < distCentre
                         ? knotCentreIndex
                         : distB < distCentre
                             ? knotCentreIndex
                             : distA > distB
                                 ? knotBIndex
                                 : knotAIndex;*/

            //Profiler.EndSample();
        }

        private void GetSearchKnotPairIndexes2(float3 vector, int first, int last, out int start, out int target)
        {
            //Profiler.BeginSample("GetSearchKnotPairIndexes2");
            /*if (knotData.Length < 3)
            {
                start  = 0;
                target = knotData.Length - 1;
            }*/


            var knotAIndex      = first;
            var knotCentreIndex = last / 2;
            var knotBIndex      = last;
            var knotA           = knotData[knotAIndex].Position;
            knotA.y = 0;
            var knotCentre = knotData[knotCentreIndex].Position;
            knotCentre.y = 0;
            var knotB = knotData[knotBIndex].Position;
            knotB.y = 0;
            var distA      = math.distance(vector, knotA);
            var distCentre = math.distance(vector, knotCentre);
            var distB      = math.distance(vector, knotB);
            start = distA < distCentre
                        ? knotAIndex
                        : distB < distCentre
                            ? knotBIndex
                            : knotCentreIndex;
            target = distA < distCentre
                         ? knotCentreIndex
                         : distB < distCentre
                             ? knotCentreIndex
                             : distA > distB
                                 ? knotBIndex
                                 : knotAIndex;

            //Profiler.EndSample();
        }

        public void CalculateHeightMapBounds(out float4 maxDistances)
        {
            // We need to get the extents of our min max area.
            // \      /
            //  \    /
            //   \__/
            // this is going to be the maximum of end and side up/down extents added to current bounds

            // So first we need to get the opposite of the triangle
            // A|\
            //  | \ c
            // a|  \
            // B|___\ C  <-- The bottom line b, we know B C a   B = 90, C = angle from constraints, a=max depth/rise
            //    b
            //Profiler.BeginSample("CalculateHeightMapBounds");
            var constraints = lineProfile.TerrainConstraints;
            // we need the max A for each
            maxDistances = new float4
                           {
                               x = GetOpposite(constraints.EndCuttingAngle, constraints.MaxDepth),
                               y = GetOpposite(constraints.EndRiseAngle, constraints.MaxRise),
                               z = GetOpposite(constraints.SideCuttingAngle, constraints.MaxDepth),
                               w = GetOpposite(constraints.SideRiseAngle, constraints.MaxRise)
                           };

            var max = math.max(maxDistances.x, math.max(maxDistances.y, math.max(maxDistances.z, maxDistances.w)));
            bounds.c1.x += max;
            //bounds.c1.z += max;

            //Profiler.EndSample();
        }

        private float GetOpposite(float angle, float maxDist)
        {
            // tan (180-90-angle) = O/A
            // tan (180-90-angle) * A = O
            return math.tan(90 - angle) * maxDist;
        }
    }
}