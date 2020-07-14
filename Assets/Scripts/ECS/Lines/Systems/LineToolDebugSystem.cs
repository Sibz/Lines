using Sibz.Lines.ECS.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Sibz.Lines.ECS.Systems
{
    [UpdateInGroup(typeof(LineWorldPresGroup), OrderLast = true)]
    public class LineToolDebugSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireSingletonForUpdate<LineTool>();
        }

        protected override void OnUpdate()
        {
            var tool = GetSingleton<LineTool>();

            Debug.DrawLine(tool.Data.Bezier1.c0, (Vector3) tool.Data.Bezier1.c0 + Vector3.up, Color.blue);
            Debug.DrawLine(tool.Data.Bezier1.c1, (Vector3) tool.Data.Bezier1.c1 + Vector3.up, Color.cyan);
            Debug.DrawLine(tool.Data.Bezier1.c2, (Vector3) tool.Data.Bezier1.c2 + Vector3.up, Color.blue);
            Debug.DrawLine(tool.Data.Bezier2.c0, (Vector3) tool.Data.Bezier2.c0 + Vector3.up, Color.blue);
            Debug.DrawLine(tool.Data.Bezier2.c1, (Vector3) tool.Data.Bezier2.c1 + Vector3.up, Color.cyan);
            Debug.DrawLine(tool.Data.Bezier2.c2, (Vector3) tool.Data.Bezier2.c2 + Vector3.up, Color.blue);

            if (EntityManager.Exists(tool.Data.LineEntity))
            {
                var buff = EntityManager.GetBuffer<LineKnotData>(tool.Data.LineEntity);
                for (var i = 0; i < buff.Length; i++)
                {
                    Vector3 p = buff[i].Position;
                    Debug.DrawLine(p, p + Vector3.up * i / (buff.Length - 1), Color.yellow);
                    if (i != buff.Length - 1)
                    {
                        var color = new Color
                                    {
                                        a = 1,
                                        b = math.lerp(1, 0, (float) i / (buff.Length - 1)),
                                        g = math.lerp(1, 0, (float) i / (buff.Length - 1)),
                                        r = math.lerp(0, 1, (float) i / (buff.Length - 1))
                                    };
                        Debug.DrawLine(p + Vector3.up * i / (buff.Length - 1),
                                       (Vector3) buff[i + 1].Position + Vector3.up * i / (buff.Length - 1), color);
                    }
                }
            }
        }
    }
}