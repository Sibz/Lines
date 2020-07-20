using Sibz.Lines.ECS.Behaviours;
using Sibz.Lines.ECS.Components;
using Unity.Entities;
using UnityEngine;

namespace Sibz.Lines.ECS.Jobs
{
    public class LineDestroySystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
               .WithStructuralChanges()
               .WithAll<Destroy>()
               .WithoutBurst()
               .ForEach((Entity entity, int entityInQueryIndex, ref Line line) =>
                        {
                            var lineObject = EntityManager.GetComponentObject<EcsLineBehaviour>(entity);
                            EntityManager.DestroyEntity(line.JoinPointA);
                            EntityManager.DestroyEntity(line.JoinPointB);
                            EntityManager.DestroyEntity(entity);
                            var ent = EntityManager.CreateEntity();
                            EntityManager.AddComponentData(ent, new RemoveHeightMap {HeightMapOwner = entity});
                            if (lineObject != null) Object.Destroy(lineObject.gameObject);
                        }).Run();
        }
    }
}