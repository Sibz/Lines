using Sibz.Lines.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Sibz.Lines.Tools.Systems
{
    [UpdateInGroup(typeof(LineSystemGroup), OrderLast = true)]
    public class DebugDrawlLinesSystem : SystemBase
    {
        private EntityQuery sectionsQuery;
        private EntityQuery linesQuery;
        protected override void OnCreate()
        {
            sectionsQuery = GetEntityQuery(typeof(LineSection));
            linesQuery = GetEntityQuery(typeof(Line2));
        }

        protected override void OnUpdate()
        {
            using (var sections =
                sectionsQuery.ToComponentDataArrayAsync<LineSection>(Allocator.TempJob, out JobHandle jh))
            {
                jh.Complete();
                for (int i = 0; i < sections.Length; i++)
                {
                    var s = sections[i];
                    Debug.DrawLine(s.Bezier.c0, (Vector3)s.Bezier.c0 + Vector3.up * 0.75f, Color.blue);
                    Debug.DrawLine(s.Bezier.c1, (Vector3)s.Bezier.c1 + Vector3.up * 0.75f, Color.cyan);
                    Debug.DrawLine(s.Bezier.c2, (Vector3)s.Bezier.c2 + Vector3.up * 0.75f, Color.blue);
                }
            }

            using (var lines = linesQuery.ToEntityArrayAsync(Allocator.TempJob, out JobHandle jh2))
            {
                jh2.Complete();
                var knotBuffers = GetBufferFromEntity<LineKnotData>();
                int len = lines.Length;
                for (int i = 0; i < len; i++)
                {
                    var buf = knotBuffers[lines[i]];
                    int len2 = buf.Length;
                    for (int j = 0; j < len2; j++)
                    {
                        Debug.DrawLine(buf[j].Knot, (Vector3)buf[j].Knot + Vector3.up * 0.25f, Color.yellow);
                    }
                }
            }
        }
    }
}