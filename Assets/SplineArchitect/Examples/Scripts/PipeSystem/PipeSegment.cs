using System.Collections.Generic;

using UnityEngine;

using SplineArchitect.Objects;
using SplineArchitect.Utility;

namespace SplineArchitect.Examples
{
    public class PipeSegment
    {
        //General
        private bool linked;
        private Spline spline;
        private List<PipeObject> pipeObjects = new List<PipeObject>();

        //Public
        public Spline Spline => spline;
        public Segment frontSegement;
        public Segment backSegment;

        public PipeSegment(Vector3 point, 
                           Vector3 tangentDirection, 
                           float tangentDistance, 
                           bool linked, 
                           List<PipeObject> pipeObjects, 
                           int index, 
                           Transform parent)
        {
            //Set data
            foreach (PipeObject item in pipeObjects)
            {
                PipeObject newItem = new PipeObject();
                newItem.prefab = item.prefab;
                newItem.offset = item.offset;
                newItem.deform = item.deform;
                newItem.snapToEnd = item.snapToEnd;
                this.pipeObjects.Add(newItem);
            }
            this.linked = linked;

            //Create spline
            GameObject splineGo = new GameObject($"PipeSegment {index}");
            splineGo.transform.parent = parent;
            spline = splineGo.AddComponent<Spline>();
            spline.componentMode = ComponentMode.ACTIVE;
            backSegment = spline.CreateSegment(point, point - tangentDirection * tangentDistance, point + tangentDirection * tangentDistance);
            point += tangentDirection * (tangentDistance * 1.25f);
            frontSegement = spline.CreateSegment(point, point - tangentDirection * tangentDistance, point + tangentDirection * tangentDistance);
    #if UNITY_EDITOR
            spline.drawInGame = true;
    #endif
            spline.SetResolutionSplineData(SAPipelineHandler.instance.splineResolution);
            spline.updateType = Spline.UpdateType.LATE_UPDATE;
        }

        public void UpdatePosition(Vector3 point)
        {
            frontSegement.SetAnchorPosition(point);

            float distance = Vector3.Distance(backSegment.GetPosition(Segment.ControlHandle.ANCHOR),
                                              frontSegement.GetPosition(Segment.ControlHandle.ANCHOR));
            Vector3 anchor1 = frontSegement.GetPosition(Segment.ControlHandle.ANCHOR);
            Vector3 anchor2 = backSegment.GetPosition(Segment.ControlHandle.ANCHOR);
            Vector3 tangentA2 = backSegment.GetPosition(Segment.ControlHandle.TANGENT_A);

            Vector3 direction1 = (tangentA2 - anchor1).normalized;
            Vector3 direction2 = backSegment.GetDirection(Segment.ControlHandle.TANGENT_B);

            if (linked)
            {
                backSegment.SetBrokenPosition(Segment.ControlHandle.TANGENT_A, anchor2 + direction2 * (distance * 0.65f));
            }
            else
            {
                Vector3 dir = (anchor1 - anchor2).normalized;
                backSegment.SetPosition(Segment.ControlHandle.TANGENT_A, anchor2 + dir);
                backSegment.SetPosition(Segment.ControlHandle.TANGENT_B, anchor2 + -dir * 10);
            }

            frontSegement.SetPosition(Segment.ControlHandle.TANGENT_A, anchor1 + -direction1 * (distance * 0.25f));
            frontSegement.SetContinuousPosition(Segment.ControlHandle.TANGENT_B, anchor1 + direction1 * (distance * 0.25f));

            //We need to update the splines cached data directly here, else we
            //dont have the correct data when running UpdateSplineObjects();
            spline.UpdateCachedData();
            //Fill the spline
            UpdateSplineObjects();
            //We should not force update the spline here, only the spline objects.
            //If we force update the spline, spline.UpdateCachedData() will run two times.
            spline.monitor.ForceUpdateSplineObjects();
        }

        public void LinkFront(Segment segment)
        {
            //Values
            Vector3 newAnchor = segment.GetPosition(Segment.ControlHandle.ANCHOR);
            Vector3 newTangentA = segment.GetPosition(Segment.ControlHandle.TANGENT_A);
            Vector3 newTangentB = segment.GetPosition(Segment.ControlHandle.TANGENT_B);

            if (segment.indexInSpline == 1)
            {
                Vector3 p = newTangentB;
                newTangentB = newTangentA;
                newTangentA = p;
            }

            //Align
            frontSegement.SetPosition(Segment.ControlHandle.ANCHOR, newAnchor);
            frontSegement.SetPosition(Segment.ControlHandle.TANGENT_A, newTangentA);
            frontSegement.SetPosition(Segment.ControlHandle.TANGENT_B, newTangentB);

            //Link
            frontSegement.linkTarget = Segment.LinkTarget.ANCHOR;
            segment.linkTarget = Segment.LinkTarget.ANCHOR;
            frontSegement.LinkToAnchor(newAnchor, false);

            if(frontSegement.indexInSpline == 1)
            {
                spline.UpdateCachedData();
                UpdateSplineObjects();
                spline.monitor.ForceUpdateSplineObjects();
            }
        }

        public void LinkBack(Segment segment)
        {
            //Values
            Vector3 newAnchor = segment.GetPosition(Segment.ControlHandle.ANCHOR);
            Vector3 newTangentA = segment.GetPosition(Segment.ControlHandle.TANGENT_B);
            Vector3 newTangentB = segment.GetPosition(Segment.ControlHandle.TANGENT_A);

            //Align
            backSegment.SetPosition(Segment.ControlHandle.ANCHOR, newAnchor);
            backSegment.SetPosition(Segment.ControlHandle.TANGENT_A, newTangentA);
            backSegment.SetPosition(Segment.ControlHandle.TANGENT_B, newTangentB);

            //Link
            backSegment.linkTarget = Segment.LinkTarget.ANCHOR;
            segment.linkTarget = Segment.LinkTarget.ANCHOR;
            backSegment.LinkToAnchor(newAnchor, false);
            linked = true;
        }

        public void UpdateSplineObjects()
        {
            foreach (PipeObject dObject in pipeObjects)
            {
                if(spline.isInvalidShape)
                {
                    foreach(SplineObject so in dObject.activeContainer)
                        so.gameObject.SetActive(false);
                }
                else
                {
                    foreach (SplineObject so in dObject.activeContainer)
                        so.gameObject.SetActive(true);

                    //GEt the global pool for the specific prefab.
                    //Can be used on any spline or any segment as long its the same original prefab.
                    List<SplineObject> pool = SAPipelineHandler.instance.Pools[dObject.prefab];

                    spline.PopulateUsingPool(dObject.activeContainer,
                                             pool,
                                             dObject.prefab,
                                             dObject.GetBounds(),
                                             Quaternion.identity,
                                             dObject.offset,
                                             0,
                                             0,
                                             dObject.snapToEnd,
                                             dObject.deform,
                                             500);
                }
            }
        }

        public void CreateStartHolder()
        {
            GameObject goHolder = Object.Instantiate(SAPipelineHandler.instance.holderPrefab);
            spline.CreateFollower(goHolder, SAPipelineHandler.instance.holderOffset, Quaternion.identity);
        }

        public void CreateEndHolder()
        {
            GameObject goHolder = Object.Instantiate(SAPipelineHandler.instance.holderPrefab);
            spline.CreateFollower(goHolder, new Vector3(0,0,spline.length) + SAPipelineHandler.instance.holderOffset, Quaternion.identity);
        }
    }
}
