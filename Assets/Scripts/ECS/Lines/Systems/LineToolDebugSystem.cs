using Sibz.Lines.ECS.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Sibz.Lines.ECS.Systems
{
    public class LineToolDebugSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireSingletonForUpdate<LineTool>();
        }

        protected override void OnUpdate()
        {
            var tool = GetSingleton<LineTool>();

            if (EntityManager.Exists(tool.Data.LineEntity))
            {
                var buff = EntityManager.GetBuffer<LineKnotData>(tool.Data.LineEntity);
                for (int i = 0; i < buff.Length; i++)
                {
                    Vector3 p = buff[i].Position;
                    Debug.DrawLine(p, p + Vector3.up * 0.5f, Color.yellow);
                }
            }

            Debug.DrawLine(tool.Data.Bezier1.c0, (Vector3)tool.Data.Bezier1.c0 + Vector3.up, Color.blue);
            Debug.DrawLine(tool.Data.Bezier1.c1, (Vector3)tool.Data.Bezier1.c1 + Vector3.up, Color.cyan);
            Debug.DrawLine(tool.Data.Bezier1.c2, (Vector3)tool.Data.Bezier1.c2 + Vector3.up, Color.blue);
            Debug.DrawLine(tool.Data.Bezier2.c0, (Vector3)tool.Data.Bezier2.c0 + Vector3.up, Color.blue);
            Debug.DrawLine(tool.Data.Bezier2.c1, (Vector3)tool.Data.Bezier2.c1 + Vector3.up, Color.cyan);
            Debug.DrawLine(tool.Data.Bezier2.c2, (Vector3)tool.Data.Bezier2.c2 + Vector3.up, Color.blue);
        }
    }
}