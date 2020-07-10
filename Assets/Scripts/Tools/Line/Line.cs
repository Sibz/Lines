using System;
using Unity.Mathematics;
using UnityEngine;

namespace Sibz.Lines
{
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