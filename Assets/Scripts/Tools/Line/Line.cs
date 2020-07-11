using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Sibz.Lines.Mono
{
    public class Line
    {
        public float Length =>
            (lineBehaviour.OriginNode.transform.localPosition - lineBehaviour.EndNode.transform.localPosition)
            .magnitude;

        public float CurveLength
        {
            get
            {
                if (SplineKnots == null || SplineKnots.Length < 2)
                    return Length;
                float distance = 0;
                for (int i = 1; i < SplineKnots.Length; i++)
                {
                    distance += math.distance(SplineKnots[i - 1], SplineKnots[i]);
                }

                return distance;
            }
        }

        private float KnotSpacing => lineBehaviour.KnotSpacing;

        public bool CentreNodeEnabled { get; set; }

        private readonly LineBehaviour lineBehaviour;
        private GameObject LineObject => lineBehaviour.gameObject;
        private readonly GameObject cursor;

        private readonly BoxCollider centreNodeActivatorCollider;
        private readonly Collider originNodeCollider;
        private readonly Collider endNodeCollider;
        private readonly Collider centreNodeCollider;
        private readonly MeshFilter meshFilter;
        private bool centreNodeIsNoneCentre;
        //private GameObject originSnappedTo;
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
            cursor = lineBehaviour.Cursor;
        }

        /*
        /*
        public void CompleteCreation()
        {
            NodeCollidersEnabled = true;
            ResizeCentreNodeActivatorCollider();
        }
        #1#

        public void MoveEndNode(Vector3 newNodePosition, GameObject nodeToMove = null)
        {
            nodeToMove = nodeToMove == null ? lineBehaviour.EndNode : nodeToMove;

            GameObject otherNode =
                nodeToMove != lineBehaviour.EndNode ? lineBehaviour.EndNode : lineBehaviour.OriginNode;
            Quaternion otherNodeWorldRotation = otherNode.transform.rotation;
            Vector3 otherNodeWorldPosition = otherNode.transform.position;
            Vector3 midWayPoint = Vector3.Lerp(newNodePosition, otherNodeWorldPosition, 0.5f);
            LineObject.transform.position = midWayPoint;
            LineObject.transform.LookAt(newNodePosition);

            otherNode.transform.position = otherNodeWorldPosition;
            otherNode.transform.rotation = otherNodeWorldRotation;
            Debug.DrawLine(otherNodeWorldPosition, otherNodeWorldPosition + otherNode.transform.forward, Color.magenta);
            nodeToMove.transform.position = newNodePosition;
            nodeToMove.transform.rotation = LineObject.transform.rotation;

            centreNodeIsNoneCentre = true;

            //UpdateKnots();
        }*/

        /*
        public void UpdateKnots(float3x4 curve, float knotSpacing)
        {
            int numberOfKnots = (int)math.ceil(Length / knotSpacing) + 2;
            SplineKnots = new float3 [numberOfKnots];

            float3 knotStart = lineBehaviour.OriginNode.transform.localPosition;
            float3 knotEnd = lineBehaviour.EndNode.transform.localPosition;

            Debug.DrawLine(lineBehaviour.transform.TransformPoint(knotStart),
                lineBehaviour.transform.TransformPoint(knotStart) + Vector3.up, Color.cyan, 0.25f);
            Debug.DrawLine(lineBehaviour.transform.TransformPoint(knotEnd),
                lineBehaviour.transform.TransformPoint(knotEnd) + Vector3.up, Color.cyan, 0.25f);

            for (int i = 0; i < numberOfKnots; i++)
            {

                /*float3x4 curve = new float3x4
                {
                    c0 = knotStart,
                    c1 = math.lerp(knotStart, knotEnd, 0.33f) + (float3)Vector3.left,
                    c2 = math.lerp(knotStart, knotEnd, 0.66f) + (float3)Vector3.left,
                    c3 = knotEnd
                };#1#
                float3 knot = Bezier.GetVectorOnCurve(curve, (float)i/(numberOfKnots - 1));
                SplineKnots[i] = knot;
                Debug.DrawLine(lineBehaviour.transform.TransformPoint(knot),
                    lineBehaviour.transform.TransformPoint(knot) + Vector3.up, Color.yellow, 0.25f);
            }

        }
        */



        public void RebuildMesh()
        {
            /*meshFilter.sharedMesh = LineMeshMaker.Build(
                new[] { lineBehaviour.OriginNode.transform.localPosition, lineBehaviour.EndNode.transform.localPosition },
                lineBehaviour.Width,
                1);*/
            meshFilter.sharedMesh = LineMeshMaker.Build(
                 lineBehaviour.transform.InverseTransformDirection(lineBehaviour.OriginNode.transform.forward),
                 lineBehaviour.transform.InverseTransformDirection(lineBehaviour.EndNode.transform.forward),
                // lineBehaviour.OriginNode.transform.forward,
                // lineBehaviour.EndNode.transform.forward,
                SplineKnots,
                lineBehaviour.Width
            );
        }

        /*private void ResizeCentreNodeActivatorCollider()
        {
            centreNodeActivatorCollider.size =
                new Vector3(
                    lineBehaviour.Width,
                    Mathf.Abs(lineBehaviour.OriginNode.transform.localPosition.y -
                              lineBehaviour.EndNode.transform.localPosition.y),
                    Length - lineBehaviour.CentreSnapPadding * 2);
        }*/

        /*public void UpdateCentreNodePosition()
        {
            if (!CentreNodeEnabled && !centreNodeIsNoneCentre)
            {
                return;
            }

            if (!CentreNodeEnabled)
            {
                lineBehaviour.CentreNode.transform.position = lineBehaviour.transform.position;
                centreNodeIsNoneCentre = false;
                return;
            }

            centreNodeIsNoneCentre = true;
            Transform tx = lineBehaviour.CentreNode.transform;
            Vector3 localPosition = tx.localPosition;
            float len = Length / 2 - lineBehaviour.CentreSnapPadding;
            tx.localPosition = new Vector3(
                localPosition.x,
                localPosition.y,
                Mathf.Clamp(lineBehaviour.transform.InverseTransformPoint(cursor.transform.position).z, -len, len));
        }*/
    }
}