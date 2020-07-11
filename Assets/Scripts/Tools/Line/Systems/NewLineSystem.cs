using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Sibz.Lines.Systems
{
    public class NewLineSystem : SystemBase
    {
        private GameObject prefab = Resources.Load<GameObject>("Prefabs/Line");
        private EntityQuery query;

        protected override void OnCreate()
        {
            query = GetEntityQuery(typeof(NewLine), typeof(Line2));
            RequireForUpdate(query);
        }

        protected override void OnUpdate()
        {
            var entities = query.ToEntityArrayAsync(Allocator.TempJob, out JobHandle jh1);
            var lines = query.ToComponentDataArrayAsync<Line2>(Allocator.TempJob, out JobHandle jh2);
            JobHandle.CombineDependencies(jh1,jh2).Complete();

            for (int i = 0; i < entities.Length; i++)
            {
                EntityManager.RemoveComponent<NewLine>(entities[i]);
                EntityManager.AddComponentObject(
                    entities[i],
                    Object.Instantiate(prefab, lines[i].Position, Quaternion.identity));
                //EntityManager.GetComponentObject<GameObject>(entities[i]);
            }

            entities.Dispose();
            lines.Dispose();

        }
    }
}