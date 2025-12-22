using System.Collections.Generic;

using UnityEngine;

using SplineArchitect.Objects;
using SplineArchitect.Utility;

namespace SplineArchitect.Examples
{
    public class SAMoveSpline : MonoBehaviour
    {
        public int segment;
        public float speed;
        public float distanceToTangents;
        public List<Transform> points;

        private Spline spline;
        private int currentPoint;
        private float time;
        private Vector3 fromAnchor;
        private Vector3 fromTangentA;
        private Vector3 fromTangentB;
        private Vector3 toPosition;
        private Vector3 toForward;

        void Start()
        {
            spline = GetComponent<Spline>();
            fromAnchor = spline.segments[segment].GetPosition(Segment.ControlHandle.ANCHOR);
            fromTangentA = spline.segments[segment].GetPosition(Segment.ControlHandle.TANGENT_A);
            fromTangentB = spline.segments[segment].GetPosition(Segment.ControlHandle.TANGENT_B);
            toPosition = points[currentPoint].transform.position;
            toForward = points[currentPoint].transform.forward;
        }

        void Update()
        {
            Vector3 anchor = spline.segments[segment].GetPosition(Segment.ControlHandle.ANCHOR);

            if (!GeneralUtility.IsEqual(anchor, points[currentPoint].transform.position))
            {
                float easing = EasingUtility.EvaluateEasing(Mathf.Clamp01(time), Easing.EASE_IN_OUT_SINE);

                Vector3 newAnchor = Vector3.Lerp(fromAnchor, toPosition, easing);
                Vector3 newTangentA = Vector3.Lerp(fromTangentA, toPosition - toForward * distanceToTangents, easing);
                Vector3 newTangentB = Vector3.Lerp(fromTangentB, toPosition + toForward * distanceToTangents, easing);
                spline.segments[segment].SetPosition(Segment.ControlHandle.ANCHOR, newAnchor);
                spline.segments[segment].SetPosition(Segment.ControlHandle.TANGENT_A, newTangentA);
                spline.segments[segment].SetPosition(Segment.ControlHandle.TANGENT_B, newTangentB);
                spline.monitor.ForceUpdate();
            }
            else
            {
                currentPoint++;
                if(currentPoint >= points.Count)currentPoint = 0;

                fromAnchor = spline.segments[segment].GetPosition(Segment.ControlHandle.ANCHOR);
                fromTangentA = spline.segments[segment].GetPosition(Segment.ControlHandle.TANGENT_A);
                fromTangentB = spline.segments[segment].GetPosition(Segment.ControlHandle.TANGENT_B);
                toPosition = points[currentPoint].transform.position;
                toForward = points[currentPoint].transform.forward;
                time = 0;
            }

            time += Time.deltaTime * speed;
        }
    }
}
