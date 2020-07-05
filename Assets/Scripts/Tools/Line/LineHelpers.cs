using System;
using Unity.Mathematics;
using UnityEngine;

namespace Sibz.Lines
{
    public static class LineHelpers
    {
        public static void GetSlopeInterceptForm(float2 origin, float2 direction, out float3 slopeIntercept)
        {
            direction = origin + direction;
            //Debug.Log($"Origin: {origin}  Dir: {direction}");
            float3 result = new float3(direction.y - origin.y, origin.x - direction.x, 0f);
            result.z = result.x * origin.x + result.y * origin.y;
            slopeIntercept = result;
        }

        public static bool TransformsCrossPaths2D(Transform tx1, Transform tx2)
        {
            float3 pos1 = tx1.position;
            float3 dir1 = tx1.forward;
            float3 pos2 = tx2.position;
            float3 dir2 = tx2.forward;
            return LineLineIntersection(out Vector3 intersection, pos1, dir1, pos2, dir2);
            /*GetSlopeInterceptForm(new float2(pos1.x, pos1.z), new float2(dir1.x, dir1.z), out float3 a);
            GetSlopeInterceptForm(new float2(pos2.x, pos2.z), new float2(dir2.x, dir2.z), out float3 b);
            return LinesIntersect(a, b);*/
        }
        public static bool TryGetTransformPathIntersection2D(Transform tx1, Transform tx2, out Vector3 intersection)
        {
            float3 pos1 = tx1.localPosition;
            float3 dir1 = tx1.forward;
            float3 pos2 = tx2.localPosition;
            float3 dir2 = tx2.forward;
            return LineLineIntersection(out intersection, pos1, dir1, pos2, dir2);
            /*GetSlopeInterceptForm(new float2(pos1.x, pos1.z), new float2(dir1.x, dir1.z), out float3 a);
            GetSlopeInterceptForm(new float2(pos2.x, pos2.z), new float2(dir2.x, dir2.z), out float3 b);
            return LinesIntersect(a, b, out intersection);*/
        }

        public static bool LinesIntersect(float3 line1, float3 line2)
        {
            return !line1.Equals(line2) && TryGetDelta(new float2x2(line1.x, line1.y, line2.x, line2.y), out float delta);
        }

        public static bool LinesIntersect(float3 line1, float3 line2, out float2 intersection)
        {
            intersection = float2.zero;
            if (line1.Equals(line2) ||
                !TryGetDelta(new float2x2(line1.x, line1.y, line2.x, line2.y), out float delta))
            {
                return false;
            }

            intersection = new float2(
                    (line2.y * line1.z - line1.y * line2.z) / delta,
                    (line1.x * line2.z - line2.x * line1.z) / delta
                );
            return true;
        }

        private static bool TryGetDelta(float2x2 ab, out float delta)
        {
            return math.abs(delta = ab.c0.x * ab.c1.y - ab.c1.x * ab.c0.y) <= float.Epsilon;
        }

        public static bool LineLineIntersection(out Vector3 intersection, Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2)
        {

            linePoint1.y = 0;
            linePoint2.y = 0;
            lineVec1.y = 0;
            lineVec2.y = 0;

            Vector3 lineVec3 = linePoint2 - linePoint1;
            Vector3 crossVec1And2 = Vector3.Cross(lineVec1, lineVec2);
            Vector3 crossVec3And2 = Vector3.Cross(lineVec3, lineVec2);

            float planarFactor = Vector3.Dot(lineVec3, crossVec1And2);

            //is coplanar, and not parallel
            if(Mathf.Abs(planarFactor) < 0.0001f && crossVec1And2.sqrMagnitude > 0.0001f)
            {
                float s = Vector3.Dot(crossVec3And2, crossVec1And2) / crossVec1And2.sqrMagnitude;
                intersection = linePoint1 + (lineVec1 * s);
                return true;
            }
            else
            {
                intersection = Vector3.zero;
                return false;
            }
        }

        public static SideOfLine GetSideOfLineXZ(this Vector3 direction, float3 point) =>
            ((float3) direction).GetSideOfLineXZ(point);

        public static SideOfLine GetSideOfLineXZ(this float3 direction, float3 point)
        {
            return new float2(direction.x, direction.z).GetSideOfLine(new float2(point.x, point.z));
        }

        public static SideOfLine GetSideOfLine(this float2 direction, float2 test)
        {
            var dot = direction.x * -test.y + direction.y * test.x;
            if (dot > 0) return SideOfLine.Right;
            if (dot < 0) return SideOfLine.Left;
            return SideOfLine.Parallel;
        }

        public enum SideOfLine
        {
            Left,
            Right,
            Parallel
        }

        public static float SignedAngleDegrees(float3 from, float3 to, float3 axis)
        {
            float num1 = AngleDegrees(from, to);
            float num2 = (float) (from.y * (double) to.z - from.z * (double) to.y);
            float num3 = (float) (from.z * (double) to.x - from.x * (double) to.z);
            float num4 = (float) (from.x * (double) to.y - from.y * (double) to.x);
            float num5 = math.sign((float) (axis.x * (double) num2 + axis.y * (double) num3 + axis.z * (double) num4));
            return num1 * num5;
        }
        public static float AngleDegrees(float3 from, float3 to)
        {
            float num = math.sqrt(from.SqrMagnitude() *  to.SqrMagnitude());
            return (double) num < 1.00000000362749E-15 ? 0.0f : (float) math.acos((double) math.clamp(math.dot(from, to) / num, -1f, 1f)) * 57.29578f;
        }

        public static float SqrMagnitude(this float3 f3)
        {
            return (float) (f3.x * (double) f3.x + f3.y * (double) f3.y +
                            f3.z * (double) f3.z);
        }
    }
}