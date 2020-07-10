using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Sibz.Lines
{
    public struct LineStart : IComponentData
    {
        public float3 Position;
    }

    public struct LineUpdate : IComponentData
    {
        public Entity Section;
        public int Join;
        public float3 Position;
    }

    public struct BezierData
    {
        public float3x3 Origin;
        public float3x3 Centre;
        public float3x3 End;
    }

    public struct CurrentLineData
    {
        public Entity Entity;
        public BezierData SectionBeziers;
        public Entity CentralSectionEntity;
        public Entity OriginSectionEntity;
        public Entity EndSectionEntity;
    }

    public struct LineTool2 : IComponentData
    {
        public ToolState State;
        public CurrentLineData Data;

        public enum ToolState : byte
        {
            Idle,
            EditLine
        }

        public static Entity New()
        {
            return LineDataWorld.World.EntityManager.CreateEntity(typeof(LineTool2));
        }
    }
}