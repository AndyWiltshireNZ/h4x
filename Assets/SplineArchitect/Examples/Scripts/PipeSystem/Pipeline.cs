using System;
using System.Collections.Generic;

using UnityEngine;
using Object = UnityEngine.Object;

using SplineArchitect.Objects;
using SplineArchitect.Utility;

namespace SplineArchitect.Examples
{
    public class Pipeline
    {
        //General
        private Transform transform;

        //None active pipe segments
        private List<PipeSegment> inactivePipeSegments = new List<PipeSegment>();
        private PipeSegment activePipeSegment;

        //Covers
        private SplineObject startCover = null;
        private SplineObject endCover = null;

        public Pipeline(Vector3 point, int index, Transform parent)
        {
            transform = new GameObject($"Pipeline {index}").transform;
            transform.parent = parent;
            PipeSegment pipeSegment = new PipeSegment(point, 
                                                      Vector3.forward, 
                                                      10, 
                                                      false, 
                                                      SAPipelineHandler.instance.pipeObjects, 
                                                      inactivePipeSegments.Count + 1, 
                                                      transform);
            activePipeSegment = pipeSegment;
            activePipeSegment.CreateStartHolder();
            CreateCovers(activePipeSegment.Spline);
        }

        public void CreateSegment(Vector3 point)
        {
            PipeSegment lastActiveSegment = activePipeSegment;
            lastActiveSegment.CreateEndHolder();

            //Calculate data for new segment
            Vector3 anchor = activePipeSegment.frontSegement.GetPosition(Segment.ControlHandle.ANCHOR);
            Vector3 tangentB = activePipeSegment.frontSegement.GetPosition(Segment.ControlHandle.TANGENT_B);
            Vector3 tangentDirection = (tangentB - anchor).normalized;
            float tangentDistance = Vector3.Distance(anchor, tangentB);

            //Create segment
            PipeSegment segment = new PipeSegment(point, 
                                                  tangentDirection, 
                                                  tangentDistance, 
                                                  true, 
                                                  SAPipelineHandler.instance.pipeObjects, 
                                                  inactivePipeSegments.Count + 1, 
                                                  transform);

            inactivePipeSegments.Add(lastActiveSegment);

            //Set as active segment
            activePipeSegment = segment;

            //Link segments. The second parameter needs to be false, else we will link all segments at this specific point.
            //We should only link anchirs wo have linkTarget = Segment.LinkTarget.ANCHOR.
            activePipeSegment.backSegment.linkTarget = Segment.LinkTarget.ANCHOR;
            lastActiveSegment.frontSegement.linkTarget = Segment.LinkTarget.ANCHOR;
            lastActiveSegment.frontSegement.LinkToAnchor(point, false);
        }

        public void UpdateSegment(Vector3 point)
        {
            activePipeSegment.UpdatePosition(point);
            UpdateCovers();
        }

        public void Enable(Segment pressedSegment)
        {
            Vector3 anchor = pressedSegment.GetPosition(Segment.ControlHandle.ANCHOR);
            Vector3 tangentA = pressedSegment.GetPosition(Segment.ControlHandle.TANGENT_A);
            Vector3 tangentB = pressedSegment.GetPosition(Segment.ControlHandle.TANGENT_B);

            activePipeSegment.frontSegement.SetPosition(Segment.ControlHandle.ANCHOR, anchor);
            activePipeSegment.frontSegement.SetPosition(Segment.ControlHandle.TANGENT_A, tangentA);
            activePipeSegment.frontSegement.SetPosition(Segment.ControlHandle.TANGENT_B, tangentB);

            activePipeSegment.backSegment.SetPosition(Segment.ControlHandle.ANCHOR, anchor);
            activePipeSegment.backSegment.SetPosition(Segment.ControlHandle.TANGENT_A, tangentA);
            activePipeSegment.backSegment.SetPosition(Segment.ControlHandle.TANGENT_B, tangentB);
            activePipeSegment.Spline.gameObject.SetActive(true);
            activePipeSegment.Spline.enabled = true;

            foreach (PipeSegment ps in inactivePipeSegments)
                ps.Spline.enabled = true;

            //We need to update the position directly after the pipeline is enabled. Else the all old spline objects will be seen for one frame.
            activePipeSegment.UpdatePosition(anchor);
            activePipeSegment.backSegment.LinkToAnchor(anchor, true);
            UpdateCovers();
        }

        public void Disable()
        {
            //Disable components after next deformation
            foreach (PipeSegment ps in inactivePipeSegments)
                ps.Spline.DisableAfterNextDeformation(Spline.DisableMode.COMPONENT);

            activePipeSegment.backSegment.Unlink();

            //Disable GameObject after next deformation
            activePipeSegment.Spline.gameObject.SetActive(false);
            UpdateCovers();
        }

        public void LinkFrontAndDisable(Segment segment)
        {
            activePipeSegment.LinkFront(segment);
            UpdateCovers();

            activePipeSegment.Spline.DisableAfterNextDeformation(Spline.DisableMode.COMPONENT);

            foreach (PipeSegment ps in inactivePipeSegments)
                ps.Spline.DisableAfterNextDeformation(Spline.DisableMode.COMPONENT);

            inactivePipeSegments.Add(activePipeSegment);
        }

        public void LinkBack(Segment segment)
        {
            activePipeSegment.LinkBack(segment);
            UpdateCovers();
        }

        public Segment GetOverlappingSegment(Vector3 point)
        {
            if(inactivePipeSegments.Count == 0)
                return null;

            Segment back = inactivePipeSegments[0].backSegment;
            if (GeneralUtility.IsEqual(back.GetPosition(Segment.ControlHandle.ANCHOR), point) && !back.HasLinks())
                return back;

            for (int i = inactivePipeSegments.Count - 1; i >= 0; i--)
            {
                PipeSegment ps = inactivePipeSegments[i];

                Segment front = ps.frontSegement;
                if (GeneralUtility.IsEqual(front.GetPosition(Segment.ControlHandle.ANCHOR), point) && !front.HasLinks())
                    return front;
            }

            return null;
        }

        private void UpdateCovers()
        {
            //End cover
            //Update end cover
            endCover.gameObject.SetActive(false);
            for (int i = inactivePipeSegments.Count - 1; i >= 0; i--)
            {
                PipeSegment pipe = inactivePipeSegments[i];

                if(pipe.Spline.gameObject.activeInHierarchy)
                {
                    endCover.transform.parent = pipe.Spline.transform;
                    endCover.localSplinePosition.z = pipe.Spline.length;
                    endCover.gameObject.SetActive(true);
                    break;
                }
            }
            if (activePipeSegment.Spline.gameObject.activeInHierarchy)
            {
                endCover.transform.parent = activePipeSegment.Spline.transform;
                endCover.localSplinePosition.z = activePipeSegment.Spline.length;
                endCover.gameObject.SetActive(true);
            }
        }

        private void CreateCovers(Spline spline)
        {
            GameObject goHolder = Object.Instantiate(SAPipelineHandler.instance.coverPrefab);
            startCover = spline.CreateFollower(goHolder, SAPipelineHandler.instance.coverOffset, Quaternion.Euler(0, 180, 0));

            goHolder = Object.Instantiate(SAPipelineHandler.instance.coverPrefab);
            endCover = spline.CreateFollower(goHolder, SAPipelineHandler.instance.coverOffset, Quaternion.Euler(0, 0, 0));
        }
    }
}
