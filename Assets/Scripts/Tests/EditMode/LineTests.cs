/*using System;
using NUnit.Framework;
using UnityEngine;

// ReSharper disable HeapView.BoxingAllocation

// ReSharper disable ObjectCreationAsStatement

namespace Sibz.Lines.Tests
{
    public class LineTests
    {
        private class TestLine
        {
            public LineBehaviour LineBehaviour;
            public GameObject OriginNode, EndNode, CentreNode, CentreNodeActivator, Cursor;
            public Line Line;

            public TestLine()
            {
                LineBehaviour = new GameObject().AddComponent<LineBehaviour>();
                LineBehaviour.OriginNode = OriginNode = new GameObject().AddComponent<BoxCollider>().gameObject;
                LineBehaviour.EndNode = EndNode = new GameObject().AddComponent<BoxCollider>().gameObject;
                LineBehaviour.CentreNode = CentreNode = new GameObject().AddComponent<BoxCollider>().gameObject;
                LineBehaviour.CentreNodeActivator =
                    CentreNodeActivator = new GameObject().AddComponent<BoxCollider>().gameObject;
                LineBehaviour.Cursor = Cursor = new GameObject();
                Line = new Line(LineBehaviour);

                EndNode.transform.localPosition = new Vector3(0, 0, 2);
            }
        }

        private static TestLine NewLine => new TestLine();

        [Test]
        public void WhenConstructingWithoutNodesSet_ShouldThrow()
        {
            LineBehaviour lineBehaviour = new GameObject().AddComponent<LineBehaviour>();
            Assert.Throws<ArgumentException>(() => new Line(lineBehaviour));
        }

        [Test]
        public void WhenConstructingWithoutNodeColliders_ShouldThrow()
        {
            LineBehaviour lineBehaviour = new GameObject().AddComponent<LineBehaviour>();
            lineBehaviour.CentreNodeActivator = new GameObject();
            lineBehaviour.OriginNode = new GameObject();
            lineBehaviour.EndNode = new GameObject();
            lineBehaviour.CentreNode = new GameObject();

            Assert.Throws<NullReferenceException>(() => new Line(lineBehaviour));
            lineBehaviour.CentreNodeActivator.AddComponent<BoxCollider>();
            Assert.Throws<NullReferenceException>(() => new Line(lineBehaviour));
            lineBehaviour.OriginNode.AddComponent<BoxCollider>();
            Assert.Throws<NullReferenceException>(() => new Line(lineBehaviour));
            lineBehaviour.EndNode.AddComponent<BoxCollider>();
            Assert.Throws<NullReferenceException>(() => new Line(lineBehaviour));
        }

        [Test]
        public void WhenConstructingWithNodesSet_ShouldNotThrow()
        {
            Assert.DoesNotThrow(() => new Line(NewLine.LineBehaviour));
        }

        [Test]
        public void Length_ShouldGetDistanceBetweenOriginAndEnd()
        {
            TestLine testLine = NewLine;
            testLine.LineBehaviour.OriginNode.transform.localPosition = new Vector3();
            testLine.LineBehaviour.EndNode.transform.localPosition = new Vector3(0,0,5f);
            Assert.AreEqual(5, testLine.Line.Length);
        }

        [Test]
        public void WhenCentreNodeIsEnabled_ShouldMoveToCursor()
        {
            TestLine testLine = NewLine;
            testLine.Cursor.transform.position = new Vector3(0, 0, 0.1f);
            testLine.Line.CentreNodeEnabled = true;
            testLine.Line.UpdateCentreNodePosition();
            Assert.AreEqual(new Vector3(0, 0, 0.1f), testLine.CentreNode.transform.localPosition);
        }

        [Test]
        public void WhenCentreNodeIsEnabled_ShouldNotMoveToCursorBeyondPadding()
        {
            TestLine testLine = NewLine;
            testLine.Line.CentreNodeEnabled = true;
            testLine.Cursor.transform.position = new Vector3(0, 0, 1f);
            testLine.Line.UpdateCentreNodePosition();
            Assert.AreEqual(new Vector3(0, 0, testLine.Line.Length / 2 - testLine.LineBehaviour.CentreSnapPadding),
                testLine.CentreNode.transform.localPosition);
            testLine.Cursor.transform.position = new Vector3(0, 0, -1f);
            testLine.Line.UpdateCentreNodePosition();
            Assert.AreEqual(new Vector3(0, 0, -(testLine.Line.Length / 2 - testLine.LineBehaviour.CentreSnapPadding)),
                testLine.CentreNode.transform.localPosition);
        }

        [Test]
        public void WhenCentreNodeIsNoneCentreAndIsDisabled_ShouldCentre()
        {
            TestLine testLine = NewLine;
            testLine.Cursor.transform.position = new Vector3(0, 0, 0.1f);
            testLine.Line.CentreNodeEnabled = true;
            testLine.Line.UpdateCentreNodePosition();
            testLine.Line.CentreNodeEnabled = false;
            testLine.Line.UpdateCentreNodePosition();
            Assert.AreEqual(Vector3.zero, testLine.CentreNode.transform.localPosition);
        }

        [Test]
        public void WhenBeginningCreation_ShouldSetOriginNodeTransformToLineTransform()
        {
            TestLine testLine = NewLine;
            testLine.LineBehaviour.transform.position = new Vector3(1,2,3);
            testLine.LineBehaviour.transform.LookAt(new Vector3(6,7,8));
            testLine.Line.BeginCreation();
            Assert.AreEqual(testLine.LineBehaviour.transform.position, testLine.OriginNode.transform.position);
            Assert.AreEqual(testLine.LineBehaviour.transform.rotation, testLine.OriginNode.transform.rotation);
        }

        [Test]
        public void WhenBeginningCreation_ShouldDisableColliders()
        {
            TestLine testLine = NewLine;
            testLine.Line.BeginCreation();
            Assert.IsFalse(
                testLine.OriginNode.GetComponent<Collider>().enabled ||
                testLine.CentreNode.GetComponent<Collider>().enabled ||
                testLine.EndNode.GetComponent<Collider>().enabled ||
                testLine.CentreNodeActivator.GetComponent<Collider>().enabled
            );
        }

        [Test]
        public void WhenEndingCreation_ShouldEnableColliders()
        {
            TestLine testLine = NewLine;
            testLine.OriginNode.GetComponent<Collider>().enabled = false;
            testLine.CentreNode.GetComponent<Collider>().enabled = false;
            testLine.EndNode.GetComponent<Collider>().enabled = false;
            testLine.CentreNodeActivator.GetComponent<Collider>().enabled = false;

            testLine.Line.CompleteCreation();
            Assert.IsTrue(
                testLine.OriginNode.GetComponent<Collider>().enabled &&
                testLine.CentreNode.GetComponent<Collider>().enabled &&
                testLine.EndNode.GetComponent<Collider>().enabled &&
                testLine.CentreNodeActivator.GetComponent<Collider>().enabled
            );
        }

        [Test]
        public void WhenEndingCreation_ShouldResizeCentreNodeActivatorColliderUsingOptions()
        {
            TestLine testLine = NewLine;
            BoxCollider collider = testLine.CentreNodeActivator.GetComponent<BoxCollider>();

            testLine.LineBehaviour.Width = 1.5f;
            testLine.OriginNode.transform.localPosition = new Vector3(0,0f,0);
            testLine.EndNode.transform.localPosition = new Vector3(0,2f,4f);
            testLine.LineBehaviour.CentreSnapPadding = 0.5f;

            testLine.Line.CompleteCreation();

            Assert.AreEqual(1.5f, collider.size.x);
            Assert.AreEqual(2f, collider.size.y);
            Assert.AreEqual(35, (int)Mathf.Round(collider.size.z*10));
        }

        [Test]
        public void WhenMovingEndNode_ShouldUpdateEndNodePosition()
        {
            TestLine testLine = NewLine;
            testLine.Line.MoveEndNode(new Vector3(0,0,5f));
            Assert.AreEqual(new Vector3(0,0,5f), testLine.EndNode.transform.position);
            testLine.Line.MoveEndNode(new Vector3(0,0,1f), testLine.OriginNode);
            Assert.AreEqual(new Vector3(0,0,1f), testLine.OriginNode.transform.position);
        }

        [Test]
        public void WhenMovingEndNode_ShouldUpdateLinePosition()
        {
            TestLine testLine = NewLine;
            testLine.Line.MoveEndNode(new Vector3(0,0,5f));
            Assert.AreEqual(new Vector3(0,0,2.5f), testLine.LineBehaviour.transform.position);
        }

        [Test]
        public void WhenMovingEndNode_ShouldUpdateLineRotation()
        {
            TestLine testLine = NewLine;
            testLine.Line.MoveEndNode(new Vector3(0,0,-5f));
            Assert.AreEqual(new Quaternion(0,1f,0,0f), testLine.LineBehaviour.transform.rotation);
        }

        //TODO: play mode test
        /*[UnityTest]
        public IEnumerator WhenMovingEndNode_ShouldNotChangeOtherNodePosition()
        {
            TestLine testLine = NewLine;
            GameObject obj = Object.Instantiate(testLine.LineBehaviour.gameObject);
            Line line = new Line(obj.GetComponent<LineBehaviour>());
            line.Enable();
            line.MoveEndNode(new Vector3(0,0,5f));
            yield return new Update();
            Assert.AreEqual(Vector3.zero, obj.GetComponent<LineBehaviour>().OriginNode.transform.position);
            Object.DestroyImmediate(obj);
        }#1#

        [Test]
        public void WhenRebuildingMesh_ShouldMakeAQuad()
        {
            TestLine testLine = NewLine;
            testLine.Line.MoveEndNode(new Vector3(0,0,-5f));
            testLine.Line.RebuildMesh();
            Assert.AreEqual(4, testLine.LineBehaviour.GetComponent<MeshFilter>().sharedMesh.vertices.Length);
        }
    }
}*/