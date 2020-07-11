using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Sibz.Lines.Tools.Systems
{
    public class DebugDrawlLinesSystem : SystemBase
    {
        private EntityQuery sectionsQuery;
        protected override void OnCreate()
        {
            sectionsQuery = GetEntityQuery(typeof(LineSection));
            RequireForUpdate(sectionsQuery);
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
                    Debug.DrawLine(s.Bezier.c0, (Vector3)s.Bezier.c0 + Vector3.up * 1.5f, Color.blue);
                    Debug.DrawLine(s.Bezier.c1, (Vector3)s.Bezier.c1 + Vector3.up * 1.5f, Color.cyan);
                    Debug.DrawLine(s.Bezier.c2, (Vector3)s.Bezier.c2 + Vector3.up * 1.5f, Color.blue);
                }
            }
        }
    }
}