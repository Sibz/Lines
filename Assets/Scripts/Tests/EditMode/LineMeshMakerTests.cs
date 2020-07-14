using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// ReSharper disable HeapView.BoxingAllocation

namespace Sibz.Lines.Tests
{
    public class LineMeshMakerTests
    {
        private const float Width    = 0.5f;
        private const int   Sections = 1;

        [Test]
        public void WhenLessThan2Vectors_ShouldThrow()
        {
            Assert.Catch<ArgumentException>(() => LineMeshMaker.Build(new Vector3[] { }, Width, Sections));
        }

        [Test]
        public void ShouldCreateQuad()
        {
            Vector3[] linePoints = {new Vector3(0, 0, 0), new Vector3(0, 0, 1)};

            var result = LineMeshMaker.Build(linePoints, Width, Sections);

            Assert.IsTrue(IsQuad(result.vertices));
        }


        public bool IsQuad(Vector3[] vertices)
        {
            var halfWidth = (float) Math.Round(Width / 2f, 2);
            var v1        = new Vector3(halfWidth, 0, 0);
            var v2        = new Vector3(-halfWidth, 0, 0);
            var v3        = new Vector3(halfWidth, 0, 1);
            var v4        = new Vector3(-halfWidth, 0, 1);

            var failMessages = new List<string>();
            if (vertices.Length != 4) Assert.Fail($"Resulting mesh had {vertices.Length} vertices, expected 4");

            if (CompareRounded(vertices[0], v1)) failMessages.Add($"Vector 1: Expected {v1} Got: {vertices[0]}");

            if (CompareRounded(vertices[1], v2)) failMessages.Add($"Vector 2: Expected {v2} Got: {vertices[1]}");

            if (CompareRounded(vertices[2], v3)) failMessages.Add($"Vector 3: Expected {v3} Got: {vertices[2]}");

            if (CompareRounded(vertices[3], v4)) failMessages.Add($"Vector 4: Expected {v4} Got: {vertices[3]}");

            if (failMessages.Count > 0)
            {
                Assert.Fail(string.Join("\n", failMessages));
                return false;
            }

            return true;
        }

        public bool CompareRounded(Vector3 lhs, Vector3 rhs)
        {
            return (int) (lhs.x * 100) == (int) (rhs.x * 100) &&
                   (int) (lhs.y * 100) == (int) (rhs.y * 100) &&
                   (int) (lhs.z * 100) == (int) (rhs.z * 100);
        }
    }
}