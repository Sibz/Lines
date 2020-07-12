using Sibz.Lines.ECS.Components;
using Unity.Entities;
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

            Debug.DrawLine(tool.Data.Bezier1.c0, (Vector3)tool.Data.Bezier1.c0 + Vector3.up, Color.blue);
            Debug.DrawLine(tool.Data.Bezier1.c1, (Vector3)tool.Data.Bezier1.c1 + Vector3.up, Color.cyan);
            Debug.DrawLine(tool.Data.Bezier1.c2, (Vector3)tool.Data.Bezier1.c2 + Vector3.up, Color.blue);
            Debug.DrawLine(tool.Data.Bezier2.c0, (Vector3)tool.Data.Bezier2.c0 + Vector3.up, Color.blue);
            Debug.DrawLine(tool.Data.Bezier2.c1, (Vector3)tool.Data.Bezier2.c1 + Vector3.up, Color.cyan);
            Debug.DrawLine(tool.Data.Bezier2.c2, (Vector3)tool.Data.Bezier2.c2 + Vector3.up, Color.blue);
        }
    }
}