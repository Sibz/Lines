using System;
using Sibz.Lines.Mono;
using UnityEngine;

namespace Sibz.Lines
{
    [RequireComponent(typeof(MeshFilter))]
    public class LineBehaviour : MonoBehaviour
    {
        public float CentreSnapPadding = 0.75f;

        [NonSerialized]
        public GameObject Cursor;

        public float KnotSpacing = 0.25f;

        [NonSerialized]
        public Line Line;

        public GameObject OriginNode, EndNode, CentreNode, CentreNodeActivator;
        public float      Width = 0.5f;

        public bool CentreNodeEnabled
        {
            set => Line.CentreNodeEnabled = value;
        }

        private void Start()
        {
            if (!OriginNode || !EndNode || !CentreNode || !CentreNodeActivator)
                throw new NullReferenceException("Must set nodes on line prefab!");
        }

        private void OnEnable()
        {
            Cursor = GameObject.FindGameObjectWithTag("Cursor");

            if (!Cursor)
            {
                Cursor = new GameObject();
                Debug.LogWarning("Cursor object not found in scene.");
            }

            Line = new Line(this);
        }

        /*private void Update()
        {
            Line.UpdateCentreNodePosition();
        }

        public override void BeginCreation(GameObject snappedTo = null)
        {
            //Line.BeginCreation(snappedTo);
        }

        public override void CompleteCreation()
        {
            Line.CompleteCreation();
        }

        public void MergeWith(LineBehaviour other, GameObject otherNode)
        {
            MoveEndNodeAndRebuildMesh(other.EndNode.transform.position, otherNode);
            Line.UpdateCentreNodePosition();
            Line.CompleteCreation();
            Destroy(other.gameObject);
        }

        public override void MoveEndNodeAndRebuildMesh(Vector3 position, GameObject otherNode = null)
        {
            Line.MoveEndNode(position, otherNode);
            Line.RebuildMesh();
        }*/
    }
}