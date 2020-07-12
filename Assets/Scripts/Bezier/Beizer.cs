using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Sibz.Bezier
{
    public class Bezier
    {
        public Vector2[] Vectors { get; private set; }


        public Bezier(Vector2 v1, Vector2 v2, Vector2 v3, Vector2 v4) : this(new Vector2[4] { v1, v2, v3, v4 })
        {
        }

        public Bezier(Vector2 v1, Vector2 v2, Vector2 v3) : this(new Vector2[3] { v1, v2, v3 })
        {
        }

        public Bezier(Vector2[] vectors)
        {
            if (!(vectors.Length == 4 || vectors.Length == 3))
                throw new ArgumentException("Bezier constructor requires array with 3 or 4 vectors");
            Vectors = vectors;
        }

        public Vector2 GetVectorOnCurve(float t)
        {
            Vector2 a = Vector2.Lerp(Vectors[0], Vectors[1], t);
            Vector2 b = Vector2.Lerp(Vectors[1], Vectors[2], t);

            if (Vectors.Length == 3)
            {
                return Vector2.Lerp(a, b, t);
            }
            else
            {
                Vector2 c = Vector2.Lerp(Vectors[2], Vectors[3], t);
                Vector2 a2 = Vector2.Lerp(a, b, t);
                Vector2 b2 = Vector2.Lerp(b, c, t);

                return Vector2.Lerp(a2, b2, t);
            }
        }

        public static float3 GetVectorOnCurve(float3 v0, float3 v1, float3 v2, float3 v3, float t)
        {
            float3 b = math.lerp(v1, v2, t);
            return math.lerp(math.lerp(math.lerp(v0, v1, t), b, t), math.lerp(b, math.lerp(v2, v3, t), t), t);
        }

        public static float3 GetVectorOnCurve(float3x3 b, float t)
            => GetVectorOnCurve(b.c0, b.c1, b.c2, t);
        public static float3 GetVectorOnCurve(float3 v0, float3 v1, float3 v2, float t)
        {
            float3 a = math.lerp(v0, v1, t);
            float3 b = math.lerp(v1, v2, t);
            float3 c = math.lerp(a, b, t);

            return c;

            //return math.lerp(math.lerp(math.lerp(v0, v1, t), b, t), math.lerp(b, math.lerp(v2, v3, t), t), t);
        }

        public static float3 GetVectorOnCurve(float3x4 curve, float t) =>
            GetVectorOnCurve(curve.c0, curve.c1, curve.c2, curve.c3, t);
    }

    public class CircularBezierSpline
    {
        public Vector2[] Vectors { get; private set; }

        /// <summary>
        /// Tension 0-1f with 1f being most tension, 0 being very loose;
        /// Higher tension pulls the curve closer to the direct line between vectors
        /// Default is 0.66f which should yield a smooth line
        /// </summary>
        public readonly float Tension;

        public CircularBezierSpline(Vector2[] vectors, float tension = 0.66f)
        {
            Vectors = vectors;
        }

        /// <summary>
        /// Get two control points for a given vector
        /// </summary>
        /// <param name="index">index of vector</param>
        /// <returns>
        /// Two vectors to use as ctl points around given vector
        /// First point is essentially the second controlpoint from the last vector
        /// Second point is the first controlpoint for the next vector
        /// </returns>
        private Vector2[] GetControlPoints(int index)
        {
            // Array for result
            Vector2[] result = new Vector2[2];

            // This vector
            Vector2 thisVector = Vectors[index];

            // Invert our tension to use as a 'looseness' multiplier
            // Also divide by two, as maximum 'looseness' is half magnitude of vector of adjacent vector
            float looseness = (1f - Tension) / 2;

            // Get ref for previous and next vectors (knots)
            Vector2 previousVector = Vectors[index - 1 < 0 ? Vectors.Length - 1 : index - 1];
            Vector2 nextVector = Vectors[index + 1 >= Vectors.Length ? 0 : index + 1];

            // Calculate vector from previous vector to the next
            Vector2 heading = nextVector - previousVector;

            // Normalise so we can multiple by a new length based on looseness and distance to prev/next vector
            heading.Normalize();

            // first control point is in reverse direction of heading
            //  next is in direction of heading
            // Multiply by the length of the vector to next/previous vectors/knots multiplied by looseness
            result[0] = thisVector - (heading * (thisVector - previousVector).magnitude * looseness);
            result[1] = thisVector + (heading * (nextVector - thisVector).magnitude * looseness);

            return result;
        }

        private static float3x2 GetControlPoints([ReadOnly] ref NativeHashMap<int, float3> knots, int index,
            float tension = 0.66f)
        {
            float3x2 result = new float3x2(new float3(), new float3());
            float3 thisVector = knots[index];
            float looseness = (1f - tension) / 2;
            float3 previousVector = knots[index - 1 < 0 ? knots.Count() - 1 : index - 1];
            float3 nextVector = knots[index + 1 >= knots.Count() ? 0 : index + 1];
            float3 heading = math.normalize(nextVector - previousVector);
            result.c0 = thisVector - (heading * math.length(thisVector - previousVector) * looseness);
            result.c1 = thisVector + (heading * math.length(nextVector - thisVector) * looseness);
            return result;
        }

        private static float3x2 GetControlPoints([ReadOnly] ref float3[] knots, int index, float tension = 0.66f)
        {
            float3x2 result = new float3x2(new float3(), new float3());
            float3 thisVector = knots[index];
            float looseness = (1f - tension) / 2;
            float3 previousVector = knots[index - 1 < 0 ? knots.Length - 1 : index - 1];
            float3 nextVector = knots[index + 1 >= knots.Length ? 0 : index + 1];
            float3 heading = math.normalize(nextVector - previousVector);
            result.c0 = thisVector - (heading * math.length(thisVector - previousVector) * looseness);
            result.c1 = thisVector + (heading * math.length(nextVector - thisVector) * looseness);
            return result;
        }

        /// <summary>
        /// Gets a vector on a this bezier curve given T
        /// </summary>
        /// <param name="t">0-1f where 0 is first point on curve and 1 is last</param>
        /// <returns>Vector2 of the point on curve</returns>
        public Vector2 GetPointOnSpline(float t)
        {
            // Find index of first knot for the segment of bezier curve we are returning
            int firstKnotIndex =
                (int) Math.Floor(t * (Vectors.Length)); // 0.1 * 10 = 1 ; 0.5 * 10 = 5 ; 0.15 * 0 = 1.5 (1) ; 1 * 10

            // Set T relative to the segment of the bezier curve we are returning
            float newT = 0f;
            if (firstKnotIndex == Vectors.Length)
            {
                firstKnotIndex--;
                newT = 1f;
            }
            else
            {
                newT = (t * Vectors.Length) - firstKnotIndex; // 0.1 *
            }

            // Get the index of the second knot and the vector
            int secondKnotIndex = firstKnotIndex + 1 >= Vectors.Length ? 0 : firstKnotIndex + 1;
            Vector2 secondKnotVector = Vectors[secondKnotIndex];

            // Get second controlpoint for this knot
            Vector2 firstControlPoint = GetControlPoints(firstKnotIndex)[1];
            // Get first controlpoint for next knot
            Vector2 secondControlPoint = GetControlPoints(secondKnotIndex)[0];

            var bezier = new Bezier(Vectors[firstKnotIndex], firstControlPoint, secondControlPoint, secondKnotVector);
            return bezier.GetVectorOnCurve(newT);
        }

        public static float3 GetPointOnSpline([ReadOnly] ref NativeHashMap<int, float3> knots, float t,
            float tension = 0.66f)
        {
            int firstKnotIndex = (int) math.floor(t * (knots.Count()));
            float newT = 0f;
            if (firstKnotIndex == knots.Count())
            {
                firstKnotIndex--;
                newT = 1f;
            }
            else
            {
                newT = (t * knots.Count()) - firstKnotIndex; // 0.1 *
            }

            // Get the index of the second knot and the vector
            int secondKnotIndex = firstKnotIndex + 1 >= knots.Count() ? 0 : firstKnotIndex + 1;

            return Bezier.GetVectorOnCurve(
                knots[firstKnotIndex],
                GetControlPoints(ref knots, firstKnotIndex, tension).c1,
                GetControlPoints(ref knots, secondKnotIndex, tension).c0,
                knots[secondKnotIndex],
                newT
            );
        }

        public static float3 GetPointOnSpline([ReadOnly] ref float3[] knots, float t, float tension = 0.66f)
        {
            int firstKnotIndex = (int) math.floor(t * (knots.Length));
            float newT = 0f;
            if (firstKnotIndex == knots.Length)
            {
                firstKnotIndex--;
                newT = 1f;
            }
            else
            {
                newT = (t * knots.Length) - firstKnotIndex; // 0.1 *
            }

            // Get the index of the second knot and the vector
            int secondKnotIndex = firstKnotIndex + 1 >= knots.Length ? 0 : firstKnotIndex + 1;

            return Bezier.GetVectorOnCurve(
                knots[firstKnotIndex],
                GetControlPoints(ref knots, firstKnotIndex, tension).c1,
                GetControlPoints(ref knots, secondKnotIndex, tension).c0,
                knots[secondKnotIndex],
                newT
            );
        }
    }
}