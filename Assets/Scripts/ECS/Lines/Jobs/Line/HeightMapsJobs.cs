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

        private NativeList<BuildKdTree.MultiKdTree.Node> nodes;
        private NativeList<int2>                         offsetAndLengths;

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
            if (nodes.IsCreated)
                nodes.Dispose();
            if (offsetAndLengths.IsCreated)
                offsetAndLengths.Dispose();
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
            nodes            = new NativeList<BuildKdTree.MultiKdTree.Node>(Allocator.TempJob);
            offsetAndLengths = new NativeList<int2>(Allocator.TempJob);
            e.Dispose();

            var dependency2 = new CombineKnotData
                              {
                                  Entities             = entities,
                                  KnotData             = KnotData,
                                  CombinedKnotData     = combinedKnotData,
                                  KnotOffsetAndLengths = knotOffsetAndLengths
                              }.Schedule(dependency);

            var dependency3 = new BuildKdTree
                              {
                                  Entities         = entities,
                                  KnotData         = KnotData,
                                  Nodes            = nodes,
                                  OffsetAndLengths = offsetAndLengths
                              }.Schedule(dependency2);

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
                             HeightMapResolution        = HeightMapResolution,
                             Nodes                      = nodes,
                             OffsetAndLengths           = offsetAndLengths
                         }.Schedule(heightData, 8, JobHandle.CombineDependencies(dependency, dependency2));

            dependency = new UpdateEntityHeightMapData
                         {
                             Ecb = LineEndSimBufferSystem.Instance.CreateCommandBuffer().AsParallelWriter(),
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
                LineProfiles[index] = LineProfilesFromEntity.HasComponent(Lines[Entities[index]].Profile)
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

        public struct BuildKdTree : IJob
        {
            public NativeList<MultiKdTree.Node> Nodes;
            public NativeList<int2>             OffsetAndLengths;

            [ReadOnly]
            public BufferFromEntity<LineKnotData> KnotData;

            [ReadOnly]
            public NativeArray<Entity> Entities;

            public struct MultiKdTree
            {
                public NativeList<Node> Nodes;
                public NativeList<int2> OffsetAndLengths;

                public MultiKdTree(Allocator allocator)
                {
                    Nodes            = new NativeList<Node>(allocator);
                    OffsetAndLengths = new NativeList<int2>(allocator);
                }

                public MultiKdTree(NativeList<Node> nodes, NativeList<int2> offsetAndLengths)
                {
                    Nodes            = nodes;
                    OffsetAndLengths = offsetAndLengths;
                }

                public void Dispose()
                {
                    if (Nodes.IsCreated)
                        Nodes.Dispose();
                    if (OffsetAndLengths.IsCreated)
                        OffsetAndLengths.Dispose();
                }

                public void AddDataSet(NativeArray<LineKnotData> slice)
                {
                    int offset = Nodes.Length;
                    int offsetIndex = OffsetAndLengths.Length;
                    OffsetAndLengths.Add(new int2(offset, 0));

                    if (slice.Length == 0) return;

                    var medianX           = GetMedian(slice);
                    var medianZ           = GetMedian(slice, false);
                    var countBelowMedianX = GetCountBelow(slice, medianX);
                    var countBelowMedianY = GetCountBelow(slice, medianZ, false);
                    AddNode(slice, countBelowMedianX - slice.Length / 2 > countBelowMedianY - slice.Length / 2);
                    int len = Nodes.Length - offset;
                    OffsetAndLengths[offsetIndex] = new int2(offset, len);

                }

                public int AddNode(NativeArray<LineKnotData> slice, bool isX = true)
                {
                    var node = new Node
                               {
                                   IsX         = isX,
                                   ResultCount = slice.Length
                               };
                    Nodes.Add(node);
                    int index = Nodes.Length - 1;
                    if (slice.Length <= 2)
                    {
                        node.IsFinal = true;
                        for (int i = 0; i < slice.Length; i++)
                        {
                            node.Results[i] = slice[i].Position;
                        }

                        Nodes[index] = node;
                        return Nodes.Length - 1;
                    }

                    var median = GetMedian(slice, isX);
                    node.Value = median;

                    var upper = new NativeList<LineKnotData>(Allocator.Temp);
                    var lower = new NativeList<LineKnotData>(Allocator.Temp);

                    void Divide()
                    {
                        upper.Clear();
                        lower.Clear();
                        for (int i = 0; i < slice.Length; i++)
                        {
                            var pos = slice[i].Position;
                            if ((isX ? pos.x : pos.y) <= median)
                                lower.Add(slice[i]);
                            else
                            {
                                upper.Add(slice[i]);
                            }
                        }
                    }

                    Divide();

                    var upperArray = new NativeArray<LineKnotData>(upper, Allocator.Temp);
                    var lowerArray = new NativeArray<LineKnotData>(lower, Allocator.Temp);

                    node.NextNodeIndexLower = AddNode(lowerArray, !isX);
                    node.NextNodeIndexUpper = AddNode(upperArray, !isX);

                    Nodes[index] = node;
                    return index;
                }

                public float3 GetClosestPosition(int dataSetIndex, float3 fromPosition)
                {
                    var node = Nodes[OffsetAndLengths[dataSetIndex].x];
                    while (node.TryGetNextIndex(new float2(fromPosition.x, fromPosition.z), out var index))
                    {
                        node = Nodes[index];
                    }

                    if (node.ResultCount == 1)
                        return node.Results[0];

                    var result = new float3();
                    var dist   = float.MaxValue;
                    for (int i = 0; i < node.ResultCount; i++)
                    {
                        var newDist = math.distance(node[i], fromPosition);
                        if (newDist >= dist) continue;
                        dist   = newDist;
                        result = node[i];
                    }

                    return result;
                }

                private int GetCountBelow(NativeSlice<LineKnotData> slice, float median, bool useX = true)
                {
                    var count = 0;
                    for (var i = 0; i < slice.Length; i++)
                        if (useX ? slice[i].Position.x < median : slice[i].Position.z < median)
                            count++;
                        else
                            break;

                    return count;
                }

                private float GetMedian(NativeSlice<LineKnotData> array, bool useX = true)
                {
                    if (array.Length % 2 == 0)
                    {
                        int first = (int) math.floor((array.Length - 1) / 2f);
                        int last  = first + 1;

                        return (useX
                                    ? array[first].Position.x + array[last].Position.x
                                    : array[first].Position.z + array[last].Position.z) / 2;
                    }

                    return useX ? array[array.Length / 2].Position.x : array[array.Length / 2].Position.y;
                }

                public struct Node
                {
                    public float    Value;
                    public bool     IsX;
                    public bool     IsFinal;
                    public int      NextNodeIndexLower, NextNodeIndexUpper;
                    public int      ResultCount;
                    public float3x4 Results;

                    public bool TryGetNextIndex(float2 comparer, out int index)
                    {
                        index = -1;
                        if (IsFinal) return false;
                        index = (IsX ? comparer.x : comparer.y) > Value ? NextNodeIndexUpper : NextNodeIndexLower;
                        return true;
                    }

                    public float3 this[int index] => Results[index];
                }
            }

            private MultiKdTree tree;

            public void Execute()
            {
                tree = new MultiKdTree(Nodes, OffsetAndLengths);
                for (int i = 0; i < Entities.Length; i++)
                {
                    tree.AddDataSet(KnotData[Entities[i]].AsNativeArray());
                }
            }

            public struct MultiKdTreeReader
            {
                public static float3 GetClosestPosition([ReadOnly] NativeArray<MultiKdTree.Node> nodes, [ReadOnly] NativeArray<int2> offsets, int dataSetIndex, float3 fromPosition)
                {
                    if (nodes.Length == 0) return new float3(float.MaxValue);
                    var node = nodes[offsets[dataSetIndex].x];
                    while (node.TryGetNextIndex(new float2(fromPosition.x, fromPosition.z), out var index))
                    {
                        node = nodes[index];
                    }

                    if (node.ResultCount == 1)
                        return node.Results[0];

                    var result = new float3();
                    var dist   = float.MaxValue;
                    for (int i = 0; i < node.ResultCount; i++)
                    {
                        var newDist = math.distance(node[i], fromPosition);
                        if (newDist >= dist) continue;
                        dist   = newDist;
                        result = node[i];
                    }

                    return result;
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

            [ReadOnly]
            public NativeList<BuildKdTree.MultiKdTree.Node> Nodes;

            [ReadOnly]
            public NativeList<int2> OffsetAndLengths;

            [NativeDisableParallelForRestriction]
            public NativeList<float2x2> HeightData;

            [NativeDisableParallelForRestriction]
            public NativeList<bool> HeightSet;

            public int    HeightMapResolution;
            public float3 TerrainSize;

            private int  entityIndex;
            private int  index;
            private int2 heightMapPosition;

            private float3 worldPosition;

            //private LineKnotData closestKnot;
            private float3 closestKnotPosition;

            //private int          closestKnotIndex;
            private LineProfile             lineProfile;
            //private BuildKdTree.MultiKdTree tree;

            public void Execute(int i)
            {
                //tree        = new BuildKdTree.MultiKdTree(Nodes, OffsetAndLengths);
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
                //var closestPosWithoutHeight = closestKnot.Position;
                var closestPosWithoutHeight = closestKnotPosition;
                closestPosWithoutHeight.y = 0;

                var dist = math.distance(closestPosWithoutHeight, worldPosition);

                //var closestKnotHeight = closestKnot.Position.y / TerrainSize.y;
                var closestKnotHeight = closestKnotPosition.y / TerrainSize.y;
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
                var distFromEdge = dist - lineProfile.Width / 2;

                float GetCurveVector(float maxDist, float maxChange, LineProfile profile)
                {
                    var t = distFromEdge / maxDist;
                    return math.lerp(closestKnotHeight, closestKnotHeight + maxChange, t);
                    /*var pointA = new float2(0, 0);
                    var controlPoint = new float2(0, 0);
                    var pointB = new float2(0, maxChange);
                    //math.lerp(closestKnotHeight,  )
                    return Bezier.Bezier.GetVectorOnCurve(pointA, controlPoint, pointB, t).y + closestKnotHeight;*/
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
                closestKnotPosition = BuildKdTree.MultiKdTreeReader
                                                 .GetClosestPosition(Nodes, OffsetAndLengths,
                                                                                       entityIndex, worldPosition);
                //TryGetClosestKnot(knotData, 0, knotData.Length - 1, out closestKnotIndex);
                //closestKnot = knotData[closestKnotIndex];
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

                    var startPosWithoutHeight = knotData[start].Position;
                    startPosWithoutHeight.y = 0;
                    var endPosWithoutHeight = knotData[end].Position;
                    endPosWithoutHeight.y = 0;

                    if (start + 1 == end)
                    {
                        closestIndex = math.distance(startPosWithoutHeight, worldPosition) >
                                       math.distance(endPosWithoutHeight, worldPosition)
                                           ? end
                                           : start;
                        return;
                    }

                    var centre = start + (end - start) / 2;
                    var distA  = math.distance(startPosWithoutHeight, worldPosition);
                    //var distB  = math.distance(knotData[centre].Position, worldPosition);
                    var distC = math.distance(endPosWithoutHeight, worldPosition);
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

            public EntityCommandBuffer.ParallelWriter Ecb;

            public void Execute(int index)
            {
                var heightData = new NativeSlice<float2x2>(HeightData,
                                                           HeightDataOffsetAndLengths[index].x,
                                                           HeightDataOffsetAndLengths[index].y);
                var heightSetData = new NativeSlice<bool>(HeightSet,
                                                          HeightDataOffsetAndLengths[index].x,
                                                          HeightDataOffsetAndLengths[index].y);
                var heightMapBuffer = HeightMapBuffers[Entities[index]];
                heightMapBuffer.Clear();
                var len     = heightData.Length;
                var lowest  = int2.zero;
                var highest = int2.zero;

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