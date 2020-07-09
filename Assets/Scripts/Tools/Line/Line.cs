using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Sibz.Lines
{
    public class LineSection
    {
        public int3 From { get; set; }
        public int3 To { get; set; }
        public float3x3 Bezier { get; set; }
        public int End1Hash { get; }
        public int End2Hash  { get; }
        public bool IsStraight => Bezier.c1.IsCloseTo(math.lerp(Bezier.c0, Bezier.c2, 0.5f));

        private readonly int hashCode;

        public LineSection(float3x3 bezier, int3? from = null, int3? to = null)
        {
            if (bezier.Equals(float3x3.zero) || bezier.c0.Equals(float3.zero) || bezier.c2.Equals(float3.zero))
            {
                throw new InvalidOperationException("Cannot create section from zero based float3x3");
            }

            hashCode = bezier.GetHashCode();
            End1Hash = bezier.c0.GetHashCode();
            End2Hash = bezier.c2.GetHashCode();

            if (from.HasValue)
                From = from.Value;
            if (to.HasValue)
                To = to.Value;
        }

        public override int GetHashCode()
        {
            return hashCode;
        }

        public float3[] GetKnotsRelativeTo(Transform tx, int endHash, float knotSpacing = 0.25f)
        {
            if (End1Hash != endHash || End2Hash != endHash)
            {
                throw new InvalidOperationException("Hash given does not match an end");
            }

            float3x3 bezier = Bezier;
            if (endHash != End1Hash)
            {
                bezier.c0 = Bezier.c2;
                bezier.c2 = Bezier.c0;
            }
            if (IsStraight)
            {
                return new[] { Bezier.c0, Bezier.c2 };
            }

            float distanceApprox = (math.distance(Bezier.c0, Bezier.c1) + math.distance(Bezier.c2, Bezier.c1) +
                                   math.distance(Bezier.c0, Bezier.c2)) / 2;
            int numberOfKnots = (int) math.ceil(distanceApprox / knotSpacing);
            float3[] knots = new float3[numberOfKnots];
            for (int i = 0; i < numberOfKnots; i++)
            {
                float t = (float) i / (numberOfKnots - 1);
                float3 worldKnot = Helpers.Bezier.GetVectorOnCurve(bezier, t);
                knots[i] = tx.InverseTransformPoint(worldKnot);
                Debug.DrawLine(worldKnot, worldKnot + new float3(0, 1, 0), Color.blue, 0.05f);
            }

            return knots;
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

        public Line(LineBehaviour lineBehaviour)
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