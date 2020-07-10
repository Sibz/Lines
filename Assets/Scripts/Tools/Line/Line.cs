using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Sibz.Lines
{
    public static class LineDataWorld
    {
        private static World world;
        public static World World => world ?? (world = new World("LineDataWorld"));
    }
    public struct LineJoinHolder : IComponentData
    {
        public DynamicBuffer<LineJoinPoint> JoinPoints;
    }
    public struct LineJoinDefinition
    {
        public Entity LineJoinHolder;
        public int JoinIndex;
    }
    public struct LineJoinPoint : IBufferElementData
    {
        public float3 Direction;
    }
    public struct LineSection : IComponentData
    {
        public static EntityArchetype LineSectionArchetype =
            LineDataWorld.World.EntityManager.CreateArchetype(typeof(LineSection), typeof(LineJoinHolder));
        public LineJoinDefinition From { get; set; }
        public LineJoinDefinition To { get; set; }
        public float3x3 Bezier { get; set; }
        public bool IsStraight => Bezier.c1.IsCloseTo(math.lerp(Bezier.c0, Bezier.c2, 0.5f));

        private readonly int hashCode;

        public LineSection(float3x3 bezier)
        {
            if (bezier.Equals(float3x3.zero) || bezier.c0.Equals(float3.zero) || bezier.c2.Equals(float3.zero))
            {
                throw new InvalidOperationException("Cannot create section from zero based float3x3");
            }

            hashCode = bezier.GetHashCode();

            From = default;
            To = default;

            Bezier = bezier;
        }

        public override int GetHashCode()
        {
            return hashCode;
        }

        public struct GetLineSectionKnotsJob : IJob
        {
            public float KnotSpacing;
            public int JoinId;
            public LineSection Section;
            public float4x4 TransformMatrix;
            public NativeList<float3> Results;
            public void Execute()
            {
                if (JoinId > 1 || JoinId < 0)
                {
                    throw new InvalidOperationException("Invalid JoinId");
                }
                float3x3 bezier = Section.Bezier;
                if (JoinId==1)
                {
                    bezier.c0 = Section.Bezier.c2;
                    bezier.c2 = Section.Bezier.c0;
                }
                if (Section.IsStraight)
                {
                    Results.Add(bezier.c0);
                    Results.Add(bezier.c2);
                    return;
                }
                float distanceApprox = (math.distance(bezier.c0, bezier.c1) + math.distance(bezier.c2, bezier.c1) +
                                        math.distance(bezier.c0, bezier.c2)) / 2;
                int numberOfKnots = (int) math.ceil(distanceApprox / KnotSpacing);
                for (int i = 0; i < numberOfKnots; i++)
                {
                    float t = (float) i / (numberOfKnots - 1);
                    float3 worldKnot = Helpers.Bezier.GetVectorOnCurve(bezier, t);
                    Results.Add(TransformMatrix.MultiplyPoint(worldKnot));
                    //Debug.DrawLine(worldKnot, worldKnot + new float3(0, 1, 0), Color.blue, 0.05f);
                }
            }
        }

    }
    public class Line
    {
        public float Length =>
            (lineBehaviour.OriginNode.transform.localPosition - lineBehaviour.EndNode.transform.localPosition)
            .magnitude;


        private readonly LineBehaviour lineBehaviour;
        private GameObject LineObject => lineBehaviour.gameObject;
        private readonly BoxCollider centreNodeActivatorCollider;
        private readonly Collider originNodeCollider;
        private readonly Collider endNodeCollider;
        private readonly Collider centreNodeCollider;
        private readonly MeshFilter meshFilter;
        public float3[] SplineKnots { get; set; } = new float3[0];
        public bool NodeCollidersEnabled
        {
            set
            {
                originNodeCollider.enabled = value;
                endNodeCollider.enabled = value;
                centreNodeCollider.enabled = value;
                centreNodeActivatorCollider.enabled = value;
            }
        }

        private NativeHashMap<int, LineSection> sections = new NativeHashMap<int, LineSection>(1,Allocator.Persistent);


        public void AddSection(LineSection section)
        {
            int hash = section.GetHashCode();
            if (sections.ContainsKey(hash))
            {
                throw new InvalidOperationException("Unable to add line section to line has already exists in line");
            }
            sections.Add(hash, section);
            //JoinSection(section);
        }

        /*public bool TryGetJoinDetails(LineSection section, out int2 other)
        {

        }*/

        public void JoinSection(LineSection section, int endHash, int2 other)
        {

        }


        public Line(LineBehaviour lineBehaviour, LineSection? section = null)
        {
            this.lineBehaviour = lineBehaviour;
            if (!lineBehaviour.OriginNode
                || !lineBehaviour.EndNode
                || !lineBehaviour.CentreNode
                || !lineBehaviour.CentreNodeActivator)
            {
                throw new ArgumentException("Must set nodes on lineBehaviour prefab!", nameof(lineBehaviour));
            }

            centreNodeActivatorCollider = lineBehaviour.CentreNodeActivator.GetComponent<BoxCollider>();
            if (centreNodeActivatorCollider == null)
            {
                throw new NullReferenceException(
                    $"{nameof(lineBehaviour.CentreNodeActivator)} must have BoxCollider component");
            }

            originNodeCollider = lineBehaviour.OriginNode.GetComponent<Collider>();
            if (originNodeCollider == null)
            {
                throw new NullReferenceException(
                    $"{nameof(lineBehaviour.OriginNode)} must have collider component");
            }

            endNodeCollider = lineBehaviour.EndNode.GetComponent<Collider>();
            if (endNodeCollider == null)
            {
                throw new NullReferenceException(
                    $"{nameof(lineBehaviour.EndNode)} must have collider component");
            }

            centreNodeCollider = lineBehaviour.CentreNode.GetComponent<Collider>();
            if (centreNodeCollider == null)
            {
                throw new NullReferenceException(
                    $"{nameof(lineBehaviour.CentreNode)} must have collider component");
            }

            if (section.HasValue)
                sections.Add(section.Value.GetHashCode(), section.Value);

            meshFilter = LineObject.GetComponent<MeshFilter>();
        }

        public void RebuildMesh()
        {

            meshFilter.sharedMesh = LineMeshMaker.Build(
                 lineBehaviour.transform.InverseTransformDirection(lineBehaviour.OriginNode.transform.forward),
                 lineBehaviour.transform.InverseTransformDirection(lineBehaviour.EndNode.transform.forward),
                 SplineKnots,
                lineBehaviour.Width
            );
        }
    }
}