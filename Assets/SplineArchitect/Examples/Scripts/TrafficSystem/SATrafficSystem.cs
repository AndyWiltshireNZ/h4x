using System;
using System.Collections.Generic;

using UnityEngine;
using Random = UnityEngine.Random;

using SplineArchitect.Utility;
using SplineArchitect.Objects;

namespace SplineArchitect.Examples
{
    public class SATrafficSystem : MonoBehaviour
    {
        private struct VechicleData
        {
            public float speed;
            public float zExtendsRear;
            public float vechicleDistance;
        }

        [Header("Settings")]
        public int totalVechicles = 0;
        public float maxSpeed = 0;
        public float minSpeed = 0;
        public float minDistanceBetweenVechicles = 0;
        public float maxDistanceBetweenVechicles = 0;
        public float heightOffset;
        public bool performenceMode = false;

        [Header("Splines")]
        public List<Spline> splines;
        [Header("Vechicles")]
        public List<GameObject> prefabs;

        //General
        private Dictionary<SplineObject, VechicleData> vechicles = new Dictionary<SplineObject, VechicleData>();
        private List<SearchData> searchData = new List<SearchData>();
        private List<SplineObject> splineObjectsToSearch = new List<SplineObject>();
        private List<Segment> links = new List<Segment>();

        void Awake()
        {
            //Crate vechicles
            for (int i = 0; i < totalVechicles; i++)
            {
                GameObject prefab = Instantiate(prefabs[Random.Range(0, prefabs.Count)]);
                Spline spline = splines[Random.Range(0, splines.Count)];

                bool hasTrailer = prefab.GetComponent<SAVehicleAndTrailer>() != null;

                //Set speed
                float speed = Random.Range(minSpeed, maxSpeed);
                //Set vechicleDistance
                float vechicleDistance = Random.Range(minDistanceBetweenVechicles, maxDistanceBetweenVechicles);
                //Set position
                Vector3 position = new Vector3(0, heightOffset, Random.Range(0, spline.length));
                //Create splineObject
                SplineObject splineObject;
                if ((hasTrailer || !performenceMode) && prefab.GetComponent<MeshFilter>() == null && prefab.transform.childCount > 0)
                {
                    splineObject = spline.CreateDeformation(prefab, position, Quaternion.identity, false);
                    for (int i2 = 0; i2 < prefab.transform.childCount; i2++)
                    {
                        Transform child = prefab.transform.GetChild(i2);
                        SplineObject soChild = spline.CreateFollower(child.gameObject, child.localPosition, child.localRotation, false, prefab.transform);

                        if (soChild.name.Contains("wheel_holder")) soChild.lockPosition = true;
                    }
                }
                else
                {
                    splineObject = spline.CreateFollower(prefab, position, Quaternion.identity, false);
                }

                //Stop wheels from spinning when performence mode is active
                if(performenceMode)
                {
                    for (int i2 = 0; i2 < prefab.GetComponentCount(); i2++)
                    {
                        Component c = prefab.GetComponentAtIndex(i2);

                        if (c is SAVehicle)
                        {
                            if(hasTrailer)
                            {
                                SAVehicle vehicle = c as SAVehicle;
                                vehicle.wheels.Clear();
                            }
                            else
                                Destroy(c);

                            break;
                        }
                    }
                }

                //Calculate bounds for zExtendsRear
                MeshFilter[] meshFilters = prefab.GetComponentsInChildren<MeshFilter>();
                Bounds bounds = new Bounds(prefab.transform.position, new Vector3(2,2,2));
                foreach (MeshFilter mf in meshFilters)
                {
                    Bounds b = new Bounds(mf.transform.TransformPoint(mf.sharedMesh.bounds.center), mf.sharedMesh.bounds.size);
                    bounds.Encapsulate(b);
                }

                //Create vechicle
                VechicleData vechicle = new VechicleData
                {
                    speed = speed,
                    zExtendsRear = bounds.extents.z,
                    vechicleDistance = vechicleDistance
                };

                splineObjectsToSearch.Add(splineObject);
                vechicles.Add(splineObject, vechicle);
            }

            //Add traffic lights to list
            foreach(Spline spline in splines)
            {
                foreach (SplineObject so in spline.allSplineObjects)
                {
                    if (so.name.Contains("TrafficLight"))
                        splineObjectsToSearch.Add(so);
                }
            }
        }

        void Update()
        {
            foreach(KeyValuePair<SplineObject, VechicleData> item in vechicles)
            {
                SearchFlags searchFlags = SearchFlags.SEARCH_FORWARD | SearchFlags.NEED_SAME_X_POSITION | SearchFlags.SEARCH_CLOSEST_LINK_FORWARD;
                Spline spline = item.Key.splineParent;
                SplineObject so = item.Key;
                VechicleData vechicleData = item.Value;
                bool onConnection = spline.name.Contains("Connection");

                searchData.Clear();
                float speedManipulator = 1;
                spline.DistanceToClosestSplineObjectNonAlloc(searchData, so.localSplinePosition, 2, searchFlags);

                if (searchData.Count > 1)
                {
                    float distance = Mathf.Abs(searchData[1].distanceToClosest);
                    SplineObject closest = searchData[1].closest;

                    //Fix for issue when two splineObjects have the same splinePositon. The issue is that they will just stop, this fixes that.
                    if (!GeneralUtility.IsZero(distance))
                    {
                        float vechicleDistance = vechicleData.vechicleDistance;
                        if(vechicles.ContainsKey(closest)) vechicleDistance += vechicles[closest].zExtendsRear;

                        speedManipulator = (distance - vechicleDistance) / vechicleDistance;
                        speedManipulator = Mathf.Clamp01(speedManipulator);
                    }
                }

                Vector3 prevSplinePosition = so.localSplinePosition;
                so.localSplinePosition.z += vechicleData.speed * Time.deltaTime * speedManipulator;

                //SEt link flags. Skip self if on a connection spline.
                LinkFlags linkFlags = LinkFlags.NONE;
                if (onConnection) linkFlags = LinkFlags.SKIP_SELF;

                //Check for link crossings and cross to other splines
                links.Clear();
                spline.FindLinkCrossingsNonAlloc(links, so.localSplinePosition, prevSplinePosition, linkFlags, out Segment currentSegment);
                if (links.Count > 0)
                {
                    Segment fromSegment = currentSegment;
                    Segment toSegment = links[Random.Range(0, links.Count)];

                    //Switch to new spline
                    so.transform.parent = toSegment.splineParent.transform;
                    spline = toSegment.splineParent;

                    so.localSplinePosition.z = spline.CalculateLinkCrossingZPosition(so.localSplinePosition, fromSegment, toSegment);
                }

                //Reset position to spline start if going beyond the end of the spline.
                if (so.localSplinePosition.z > spline.length)
                    so.localSplinePosition.z = 0;
            }
        }
    }
}
