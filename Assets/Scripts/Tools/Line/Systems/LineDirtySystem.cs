using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Sibz.Lines.Tools.Systems
{
    public class LineDirtySystem : SystemBase
    {
        private EntityQuery lineSectionsQuery;
        private EntityQuery lineQuery;


        protected override void OnCreate()
        {
            lineSectionsQuery = GetEntityQuery(typeof(LineSection));
        }

        protected override void OnUpdate()
        {
            NativeArray<Entity> sectionEntities =
                lineSectionsQuery.ToEntityArrayAsync(Allocator.TempJob, out JobHandle jh1);
            NativeArray<LineSection> sections =
                lineSectionsQuery.ToComponentDataArrayAsync<LineSection>(Allocator.TempJob, out JobHandle jh2);

            using (NativeMultiHashMap<Entity, SectionData> sectionsByEntity =
                new NativeMultiHashMap<Entity, SectionData>(lineSectionsQuery.CalculateEntityCount(),
                    Allocator.TempJob))
            {
                var joinBuffer = GetBufferFromEntity<LineJoinPoint>(true);


                Dependency = JobHandle.CombineDependencies(Dependency,
                    new BuildHashMapJob
                    {
                        Components = sections,
                        Entities = sectionEntities,
                        ComponentsByEntity = sectionsByEntity.AsParallelWriter()
                    }.Schedule(lineSectionsQuery.CalculateEntityCount(), JobHandle.CombineDependencies(Dependency, jh1, jh2)));

                BufferFromEntity<LineKnotData> knotBuffers = GetBufferFromEntity<LineKnotData>();

                Entities
                    .WithStoreEntityQueryInField(ref lineQuery)
                    .WithAll<Dirty>()
                    .ForEach((Entity lineEntity, ref Line2 line) =>
                    {
                        new OnDirtyLineJob
                        {
                            // ReSharper disable once AccessToDisposedClosure
                            SectionsByEntity = sectionsByEntity,
                            LineEntity = lineEntity,
                            KnotBuffer = knotBuffers[lineEntity],
                            KnotSpacing = 0.25f,
                            JoinPoints = joinBuffer
                        }.Execute(ref line);
                    }).Schedule(Dependency).Complete();
            }

            EntityManager.RemoveComponent<Dirty>(lineQuery);
        }

        private struct OnDirtyLineJob
        {
            [ReadOnly] public NativeMultiHashMap<Entity, SectionData> SectionsByEntity;
            [ReadOnly] public BufferFromEntity<LineJoinPoint> JoinPoints;

            public Entity LineEntity;
            public DynamicBuffer<LineKnotData> KnotBuffer;
            public float KnotSpacing;

            private Line2 line;

            private NativeList<SectionData> sections;
            private NativeArray<Entity> sectionsEntities;


            public void Execute(ref Line2 lineComponent)
            {
                line = lineComponent;

                GetSectionsForThisLine();

                UpdateKnotData();

                lineComponent = line;
            }

            private void UpdateKnotData()
            {
                KnotBuffer.Clear();

                GetFirstSection(out SectionData section, out int index);
                Entity first = section.Entity;
                do
                {
                    GetKnots(ref section, index);
                } while (TryGetNextSection(ref section, ref index) && !section.Entity.Equals(first));

                if (section.Entity.Equals(first) && KnotBuffer.Length>0)
                {
                    var knot = KnotBuffer[KnotBuffer.Length - 1];
                    knot.Flags &= KnotFlags.End;
                    KnotBuffer[KnotBuffer.Length - 1] = knot;
                }
            }

            private void GetKnots(ref SectionData section, int index)
            {
                float3x3 bezier = section.Section.Bezier;
                if (index == 1)
                {
                    bezier.c0 = section.Section.Bezier.c2;
                    bezier.c2 = section.Section.Bezier.c0;
                }

                /*if (section.Section.IsStraight)
                {
                    KnotBuffer.Add(new LineKnotData { Knot = bezier.c0 });
                    KnotBuffer.Add(new LineKnotData { Knot = bezier.c2 });
                    return;
                }*/

                float distanceApprox = (math.distance(bezier.c0, bezier.c1) + math.distance(bezier.c2, bezier.c1) +
                                        math.distance(bezier.c0, bezier.c2)) / 2;
                int numberOfKnots = (int) math.ceil(distanceApprox / KnotSpacing);
                for (int i = 0; i < numberOfKnots; i++)
                {
                    float t = (float) i / (numberOfKnots - 1);
                    KnotBuffer.Add(new LineKnotData { Knot = Helpers.Bezier.GetVectorOnCurve(bezier, t) });
                }
            }

            private bool TryGetNextSection(ref SectionData section, ref int joinIndex)
            {
                int otherIndex = math.abs(joinIndex - 1);
                if (!JoinPoints[section.Entity][otherIndex].IsJoined
                    || !sectionsEntities.Contains(JoinPoints[section.Entity][otherIndex].JoinData.ConnectedEntity))
                {
                    return false;
                }

                section = GetSectionData(JoinPoints[section.Entity][otherIndex].JoinData.ConnectedEntity);
                joinIndex = JoinPoints[section.Entity][otherIndex].JoinData.ConnectedIndex;
                return true;
            }

            private void GetFirstSection(out SectionData section, out int joinIndex)
            {
                if (!TryGetFirstNonJoinedSection(out section))
                {
                    section = sections[0];
                }

                // If not joined at all current index is 0
                // if 1 is joined to valid section then current is 0
                // otherwise current is 1
                joinIndex =
                    !JoinPoints[section.Entity][0].IsJoined && !JoinPoints[section.Entity][1].IsJoined
                        ? 0
                        : JoinPoints[section.Entity][1].IsJoined &&
                          sectionsEntities.Contains(JoinPoints[section.Entity][1].JoinData.ConnectedEntity)
                            ? 0
                            : 1;
            }

            private bool TryGetFirstNonJoinedSection(out SectionData data)
            {
                int len = sections.Length;
                for (int i = 0; i < len; i++)
                {
                    if (JoinPoints[sectionsEntities[i]][0].IsJoined &&
                        sectionsEntities.Contains(JoinPoints[sectionsEntities[i]][0].JoinData.ConnectedEntity) &&
                        JoinPoints[sectionsEntities[i]][1].IsJoined &&
                        sectionsEntities.Contains(JoinPoints[sectionsEntities[i]][1].JoinData.ConnectedEntity)
                    )
                    {
                        continue;
                    }

                    data = sections[i];
                    return true;
                }

                data = default;
                return false;
            }

            private void GetSectionsForThisLine()
            {
                sections = new NativeList<SectionData>(Allocator.Temp);
                if (SectionsByEntity.TryGetFirstValue(LineEntity, out SectionData item,
                    out NativeMultiHashMapIterator<Entity> it))
                {
                    do
                    {
                        sections.Add(item);
                    } while (SectionsByEntity.TryGetNextValue(out item, ref it));
                }

                sectionsEntities = new NativeArray<Entity>(sections.Length, Allocator.Temp);
                int len = sections.Length;
                for (int i = 0; i < len; i++)
                {
                    sectionsEntities[i] = sections[i].Entity;
                }
            }

            private int GetSectionIndex(Entity entity)
            {
                return sectionsEntities.IndexOf<Entity>(entity);
            }

            private SectionData GetSectionData(Entity entity)
            {
                return sections[GetSectionIndex(entity)];
            }
        }

        private struct SectionData
        {
            public Entity Entity;
            public LineSection Section;
        }

        private struct BuildHashMapJob : IJobFor
        {
            [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<Entity> Entities;
            [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<LineSection> Components;
            public NativeMultiHashMap<Entity, SectionData>.ParallelWriter ComponentsByEntity;

            public void Execute(int index)
            {
                ComponentsByEntity.Add(
                    Components[index].ParentLine,
                    new SectionData
                    {
                        Section = Components[index],
                        Entity = Entities[index]
                    });
            }
        }
    }
}