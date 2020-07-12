﻿using Unity.Mathematics;

namespace Sibz.Math
{
    public static class Mathematics
    {
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