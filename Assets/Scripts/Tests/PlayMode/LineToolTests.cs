/*using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// ReSharper disable HeapView.BoxingAllocation

namespace Sibz.Lines.Tests
{
    public class LineToolTests
    {
        private class TestLinePrefab : LineBase
        {
            public bool BegunEdit, TransformedNode, EndedEdit;
            public override void BeginCreation(GameObject snappedTo = null)
            {
                BegunEdit = true;
            }

            public override void MoveEndNodeAndRebuildMesh(Vector3 position, GameObject otherNode = null)
            {
                TransformedNode = true;
                Transform transform1 = transform;
                transform1.position = position;
            }

            public override void CompleteCreation()
            {
                EndedEdit = true;
            }
        }

        private static GameObject NewCursor { get
            {
                GameObject cursor = new GameObject();
                cursor.transform.position = new Vector3(1,2,3);
                return cursor;
            }
        }

        private static GameObject NewLinePrefab
        {
            get
            {
                GameObject prefab = new GameObject();
                prefab.AddComponent<TestLinePrefab>();
                return prefab;
            }
        }

        [Test]
        public void WhenConstructedWithEitherParameterNull_ShouldThrow()
        {
            // ReSharper disable ObjectCreationAsStatement
            Assert.Throws<System.ArgumentNullException>(() => new LineTool(null, NewLinePrefab));
            Assert.Throws<System.ArgumentNullException>(() => new LineTool(NewCursor, null));
            Assert.Throws<System.ArgumentNullException>(() => new LineTool(null, null));
            // ReSharper restore ObjectCreationAsStatement
        }

        [Test]
        public void WhenStartingLine_ShouldSetCurrentLine()
        {
            LineTool tool = new LineTool(NewCursor, NewLinePrefab);
            tool.StartLine();
            Assert.IsNotNull(tool.CurrentLine);
        }

        [Test]
        public void WhenStartingLine_ShouldCallBeginEdit()
        {
            LineTool tool = new LineTool(NewCursor, NewLinePrefab);
            tool.StartLine();
            Assert.IsTrue(((TestLinePrefab)tool.CurrentLine).BegunEdit);
        }

        [Test]
        public void WhenStatingLine_ShouldSetPositionWithOffset()
        {
            GameObject cursor = NewCursor;
            LineTool tool = new LineTool(cursor, NewLinePrefab);
            tool.StartLine();
            Assert.AreEqual(cursor.transform.position + tool.Offset, tool.CurrentLine.transform.position);
        }
        [Test]

        public void WhenStatingLine_ShouldSetRotation()
        {
            GameObject cursor = NewCursor;
            LineTool tool = new LineTool(cursor, NewLinePrefab);
            tool.StartLine();
            Assert.AreEqual(cursor.transform.rotation, tool.CurrentLine.transform.rotation);
        }

        [Test]
        public void WhenUpdatingLine_ShouldCallTransformEndNode()
        {
            GameObject cursor = NewCursor;
            LineTool tool = new LineTool(cursor, NewLinePrefab);
            tool.StartLine();
            tool.UpdateLine();
            Assert.IsTrue(((TestLinePrefab)tool.CurrentLine).TransformedNode);
        }

        [Test]
        public void WhenUpdatingLine_ShouldPassCursorPosition()
        {
            GameObject cursor = NewCursor;

            LineTool tool = new LineTool(cursor, NewLinePrefab);
            tool.StartLine();
            cursor.transform.position = new Vector3(2,3,4);
            tool.UpdateLine();
            Assert.AreEqual(cursor.transform.position, tool.CurrentLine.transform.position);
        }

        /*
         Disabled as rotation only set when snapped
         [Test]
        public void WhenUpdatingLine_ShouldPassCursorRotation()
        {
            GameObject cursor = NewCursor;

            LineTool tool = new LineTool(cursor, NewLinePrefab);
            tool.StartLine();
            cursor.transform.LookAt(Vector3.down);
            tool.UpdateLine();
            Assert.AreEqual(cursor.transform.rotation, tool.CurrentLine.transform.rotation);
        }#1#

        [Test]
        public void WhenEndingLine_ShouldCallEndEdit()
        {
            LineTool tool = new LineTool(NewCursor, NewLinePrefab);
            tool.StartLine();
            TestLinePrefab line = (TestLinePrefab) tool.CurrentLine;
            tool.EndLine();
            Assert.IsTrue(line.EndedEdit);
        }

        [Test]
        public void WhenEndingLine_ShouldSetCurrentLineToNull()
        {
            LineTool tool = new LineTool(NewCursor, NewLinePrefab);
            tool.StartLine();
            tool.EndLine();
            Assert.IsNull(tool.CurrentLine);
        }

        [Test]
        public void WhenCanceledAndNoLine_ShouldNotThrow()
        {
            LineTool tool = new LineTool(NewCursor, NewLinePrefab);
            Assert.DoesNotThrow(()=>tool.Cancel());
        }

        [UnityTest]
        public IEnumerator WhenCanceled_ShouldDestroyCurrentLine()
        {
            LineTool tool = new LineTool(NewCursor, NewLinePrefab);
            tool.StartLine();
            GameObject line = tool.CurrentLine.gameObject;
            tool.Cancel();
            yield return null;
            Assert.IsTrue(line==null);
        }

        [UnityTest]
        public IEnumerator WhenCanceled_ShouldSetCurrentLineToNull()
        {
            LineTool tool = new LineTool(NewCursor, NewLinePrefab);
            tool.StartLine();
            tool.Cancel();
            yield return null;
            Assert.IsTrue(tool.CurrentLine == null);
        }

        [Test]
        public void WhenBeginningSnappedToANode_ShouldSetRotation()
        {
            LineTool tool = new LineTool(NewCursor, NewLinePrefab);
            GameObject snappedToNode = new GameObject();
            snappedToNode.transform.rotation = new Quaternion(0,1,0,0);
            tool.OriginSnappedToNode = snappedToNode;
            tool.StartLine();
            Assert.AreEqual(new Quaternion(0,1,0,0), tool.CurrentLine.transform.rotation);
        }
    }
}*/

