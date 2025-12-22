// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: NearestPointsSplinesJob.cs
//
// Author: Mikael Danielsson
// Date Created: 04-11-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using Unity.Collections;

using SplineArchitect.Objects;
using SplineArchitect.Utility;

namespace SplineArchitect.Jobs
{
    [BurstCompile]
    public struct NearestPointsSplinesJob : IJob
    {
        public NativeArray<Vector3> points;
        [ReadOnly] public NativeArray<NativeSegment> nativeSegmentsSpline1;
        [ReadOnly] public NativeList<float> distanceMapSpline1;
        [ReadOnly] public float spline1Resolution;
        [ReadOnly] public bool spline1Loop;
        [ReadOnly] public float spline1Length;
        [ReadOnly] public NativeArray<NativeSegment> nativeSegmentsSpline2;
        [ReadOnly] public NativeList<float> distanceMapSpline2;
        [ReadOnly] public float spline2Resolution;
        [ReadOnly] public bool spline2Loop;
        [ReadOnly] public float spline2Length;
        [ReadOnly] public float stepsPer100Meter;
        [ReadOnly] public int precision;

        public void Execute()
        {
            float steps1 = 100 / spline1Length / stepsPer100Meter;
            if (steps1 > 0.2f) steps1 = 0.2f;
            if (steps1 < 0.0001f) steps1 = 0.0001f;

            float steps2 = 100 / spline2Length / stepsPer100Meter;
            if (steps2 > 0.2f) steps2 = 0.2f;
            if (steps2 < 0.0001f) steps2 = 0.0001f;

            float disCheck = 99999;
            float nearestTime1 = 0.5f;
            float nearestTime2 = 0.5f;
            float range1 = 0.5f;
            float range2 = 0.5f;
            Vector3 point1 = Vector3.zero;
            Vector3 point2 = Vector3.zero;

            for (int i = 0; i < precision; i++)
            {
                float st1 = nearestTime1 - range1;
                float et1 = nearestTime1 + range1;
                float st2 = nearestTime2 - range2;
                float et2 = nearestTime2 + range2;

                for (float t = st1; t < et1 + steps1; t += steps1)
                {
                    float ft = SplineUtilityNative.TimeToFixedTime(distanceMapSpline1, spline1Resolution, Mathf.Clamp01(t), spline1Loop);
                    Vector3 pos1 = SplineUtilityNative.GetPosition(nativeSegmentsSpline1, ft);

                    for (float t2 = st2; t2 < et2 + steps2; t2 += steps2)
                    {
                        float ft2 = SplineUtilityNative.TimeToFixedTime(distanceMapSpline2, spline2Resolution, Mathf.Clamp01(t2), spline2Loop);
                        Vector3 pos2 = SplineUtilityNative.GetPosition(nativeSegmentsSpline2, ft2);

                        float dis = Vector3.Distance(pos1, pos2);

                        if (dis < disCheck)
                        {
                            disCheck = dis;
                            point1 = pos1;
                            point2 = pos2;
                            nearestTime1 = t;
                            nearestTime2 = t2;
                        }
                    }
                }

                range1 = steps1 * 2;
                range2 = steps2 * 2;
                steps1 = steps1 / 2;
                steps2 = steps2 / 2;
            }

            points[0] = point1;
            points[1] = point2;
        }
    }
}
