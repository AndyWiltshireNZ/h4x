using System;
using System.Collections.Generic;

using UnityEngine;

using SplineArchitect.Objects;
using SplineArchitect.Utility;

namespace SplineArchitect.Examples
{
    public class SAPipelineHandler : MonoBehaviour
    {
        enum State     
        {
            IDLE,
            CREATING_PIPELINE,
        }

        [Header("Indicator")]
        public GameObject indicatorPrefabIdle;
        public GameObject indicatorPrefabCreating;
        public GameObject indicatorPrefabLink;
        public Vector3 indicatorOffset;
        public LayerMask indicatorLayerMask;

        [Header("Settings")]
        public float gridSize;
        public float splineResolution;

        [Header("Cover")]
        public GameObject coverPrefab;
        public Vector3 coverOffset;

        [Header("Section holder")]
        public GameObject holderPrefab;
        public Vector3 holderOffset;

        [Header("Pipe objects")]
        public List<PipeObject> pipeObjects;

        //Indicators
        private GameObject indicatorIdle;
        private GameObject indicatorCreating;
        private GameObject indicatorLink;

        //Pool
        private Dictionary<GameObject, List<SplineObject>> pools = new Dictionary<GameObject, List<SplineObject>>();

        //General
        private Pipeline activePipeline;
        private List<Pipeline> pipelines = new();
        private Plane projectionPlane = new Plane(Vector3.up, Vector3.zero);
        private State state = State.IDLE;

        //Singelton
        public static SAPipelineHandler instance = null;

        //Public
        public Pipeline ActivePipeLine => activePipeline;
        public Dictionary<GameObject, List<SplineObject>> Pools => pools;

        private void OnEnable()
        {
            instance = this;
        }

        private void Start()
        {
            //Create global pools
            foreach(PipeObject po in pipeObjects)
                pools.Add(po.prefab, new List<SplineObject>());

            //Create indicators
            indicatorIdle = Instantiate(indicatorPrefabIdle);
            indicatorCreating = Instantiate(indicatorPrefabCreating);
            indicatorLink = Instantiate(indicatorPrefabLink);
        }

        private void Update()
        {
            Vector3 indicatorPos = GetIndicatorPosition();
            (Pipeline, Segment) overlappingData = GetOverlappingData(indicatorPos);
            Pipeline overlappingPipeline = overlappingData.Item1;
            Segment overlappingSegment = overlappingData.Item2;

            if (state == State.IDLE)
            {
                //GO TO: CREATING_SPLINE
                if (Input.GetMouseButtonDown(0))
                {
                    if (overlappingSegment != null)
                    {
                        if(overlappingSegment.indexInSpline == 1)
                        {
                            overlappingPipeline.Enable(overlappingSegment);
                            activePipeline = overlappingPipeline;
                        }
                        else
                        {
                            activePipeline = CreatePipeline();
                            activePipeline.LinkBack(overlappingSegment);
                        }
                    }
                    else
                        activePipeline = CreatePipeline();

                    state = State.CREATING_PIPELINE;
                }
            }
            else if(state == State.CREATING_PIPELINE)
            {
                //GO TO: IDLE
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    state = State.IDLE;
                    activePipeline.Disable();
                    activePipeline = null;
                    return;
                }

                activePipeline.UpdateSegment(indicatorPos);

                if (Input.GetMouseButtonDown(0))
                {
                    //GO TO: IDLE and Align pipelines 
                    if (overlappingSegment != null)
                    {
                        state = State.IDLE;
                        activePipeline.LinkFrontAndDisable(overlappingSegment);
                        activePipeline = null;
                        return;
                    }
                    //Create segment
                    else
                    {
                        activePipeline.CreateSegment(indicatorPos);
                    }
                }
            }

            UpdateIndicator(indicatorPos, overlappingSegment != null);
        }

        private void UpdateIndicator(Vector3 pos, bool hoverOverLink)
        {
            indicatorLink.gameObject.SetActive(false);
            indicatorIdle.gameObject.SetActive(false);
            indicatorCreating.gameObject.SetActive(false);

            if (hoverOverLink)
            {
                indicatorLink.transform.position = pos + indicatorOffset;
                indicatorLink.gameObject.SetActive(true);
            }
            else
            {
                if (state == State.CREATING_PIPELINE)
                {
                    indicatorCreating.transform.position = pos + indicatorOffset;
                    indicatorCreating.gameObject.SetActive(true);
                }
                else
                {
                    indicatorIdle.transform.position = pos + indicatorOffset;
                    indicatorIdle.gameObject.SetActive(true);
                }
            }
        }

        private Pipeline CreatePipeline()
        {
            Pipeline pipeline = new Pipeline(GetIndicatorPosition(), pipelines.Count + 1, transform);
            pipelines.Add(pipeline);

            return pipeline;
        }

        private (Pipeline, Segment) GetOverlappingData(Vector3 indicatorPosition)
        {
            Segment segment = null;
            Pipeline pipeline = null;

            foreach(Pipeline p in pipelines)
            {
                if (activePipeline == p)
                    continue;

                Segment s = p.GetOverlappingSegment(indicatorPosition);

                if(s != null)
                {
                    pipeline = p;
                    segment = s; 
                    break;
                }
            }

            return (pipeline, segment);
        }

        private Vector3 GetIndicatorPosition()
        {
            Vector3 pos = Vector3.zero;
            Vector3 mousePos = Input.mousePosition;

            if (float.IsNaN(mousePos.x) || float.IsNaN(mousePos.y) ||
                float.IsInfinity(mousePos.x) || float.IsInfinity(mousePos.y))
                return pos;

            Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(mouseRay, out RaycastHit hitInfo, float.MaxValue, indicatorLayerMask))
            {
                pos += hitInfo.point;
            }
            else
            {
                if (projectionPlane.Raycast(mouseRay, out float enter2))
                    pos += mouseRay.GetPoint(enter2);
            }
            pos = GeneralUtility.RoundToClosest(pos, gridSize);

            return pos;
        }
    }
}
