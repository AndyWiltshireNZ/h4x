// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: HandleDeformation.cs
//
// Author: Mikael Danielsson
// Date Created: 11-03-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections.Generic;

using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

using SplineArchitect.Jobs;
using SplineArchitect.Objects;
using SplineArchitect.Utility;

namespace SplineArchitect
{
    public class HandleDeformation
    {
        public static List<DeformationWorker> activeWorkersList = new List<DeformationWorker>();
        private static List<DeformationWorker> deformationWorkers = new List<DeformationWorker>();
        private static Dictionary<int, float4x4> localSpaces = new Dictionary<int, float4x4>();
        private static Vector3[] normals = new Vector3[3];

        /// <summary>
        /// Attempts to deform the specified Spline and its associated objects in real-time, handling both followers and deformations.
        /// </summary>
        /// <param name="spline">The Spline to be deformed in real-time.</param>
        /// <param name="deformLinks">Specifies whether linked control points should also be deformed. Default is true.</param>
        /// <remarks>
        /// This function updates and deforms SplineObjects based on various conditions, including scale, position, and rotation changes. 
        /// It processes both spline followers, which move along the spline without deforming, and spline deformers, which modify mesh data.
        /// The function handles runtime deformation checks, recalculates positions and normals, and initiates deformation jobs. 
        /// </remarks>
        public static void TryDeformRealtime(Spline spline, bool deformLinks = true)
        {
            if (spline.isInvalidShape)
                return;

            bool splineDirty = spline.monitor.IsDirty();
            if (splineDirty) spline.UpdateCachedData();

            for (int i = 0; i < spline.allSplineObjects.Count; i++)
            {
                SplineObject so = spline.allSplineObjects[i];

#if UNITY_EDITOR
                if (so == null)
                    continue;
#endif
                bool scaleChange = so.monitor.ScaleChange(true);
                bool posRotSplineSpaceChange = so.monitor.PosRotSplineSpaceChange(true);
                bool combinedParentPosRotScaleChange = so.monitor.CombinedParentPosRotScaleChange(true);
                bool soDirty = scaleChange || posRotSplineSpaceChange || combinedParentPosRotScaleChange;

#if UNITY_EDITOR
                //If not valid for runtime let the editor deformation process catch and deform it.
                //If readablility is set to false, the mesh can still be deformed during the editor deformation process.
                if ((splineDirty || soDirty) && !so.ValidForRuntimeDeformation())
                {
                    so.monitor.ForceUpdateEditor();
                    continue;
                }
#endif

                //FOLLOWER
                if (so.type == SplineObject.Type.FOLLOWER)
                {
                    if (!splineDirty && !soDirty)
                        continue;

                    spline.followerUpdateList.Add(so);
                }
                //DEFORMATION
                else if (so.type == SplineObject.Type.DEFORMATION)
                {
                    if (!splineDirty && !soDirty)
                        continue;

                    if (so.meshContainers == null || so.meshContainers.Count == 0)
                    {
                        so.transform.localPosition = so.localSplinePosition;
                        so.transform.localRotation = so.localSplineRotation;
                        so.monitor.UpdateSplineLength(spline.length);
                        continue;
                    }

                    Deform(so, DeformationWorker.Type.RUNETIME, spline);
                }
            }

            GetActiveWorkers(DeformationWorker.Type.RUNETIME, activeWorkersList);
            foreach (DeformationWorker dw in activeWorkersList)
            {
                dw.Start();
                dw.Complete();
            }

            UpdateFollowers(spline);
            if (splineDirty && deformLinks) TryDeformRealtimeLinks(spline);
        }

        /// <summary>
        /// Attempts to deform linked splines in real-time based on the changes in the specified Spline.
        /// </summary>
        /// <param name="spline">The Spline whose linked splines will be checked and deformed if necessary.</param>
        public static void TryDeformRealtimeLinks(Spline spline)
        {
            foreach (Segment s in spline.segments)
            {
                if (s.links == null)
                    continue;

                foreach (Segment s2 in s.links)
                {
                    if (s2.splineParent == null)
                        continue;

                    if (s2.splineParent == spline)
                        continue;

                    //Set link position
                    Vector3 newPosition = s.GetPosition(Segment.ControlHandle.ANCHOR);
                    Vector3 dif = s2.GetPosition(Segment.ControlHandle.ANCHOR) - newPosition;
                    s2.Translate(Segment.ControlHandle.ANCHOR, dif);
                    s2.Translate(Segment.ControlHandle.TANGENT_A, dif);
                    s2.Translate(Segment.ControlHandle.TANGENT_B, dif);

                    s2.splineParent.monitor.ForceUpdate();
                    TryDeformRealtime(s2.splineParent, false);
                }
            }
        }

        /// <summary>
        /// Updates the positions and rotations of all followers on the specified Spline.
        /// </summary>
        /// <param name="spline">The Spline whose followers will be updated.</param>
        /// <remarks>
        /// Followers are GameObjects that move along the spline without deforming it. This function recalculates 
        /// their positions and orientations based on their local spline positions and parent transformations.
        /// </remarks>
        public static void UpdateFollowers(Spline spline)
        {
            if (spline.followerUpdateList.Count == 0)
                return;

            NativeArray<Vector3> points = new NativeArray<Vector3>(spline.followerUpdateList.Count, Allocator.TempJob);
            NativeArray<int> verticesMap = new NativeArray<int>(spline.followerUpdateList.Count, Allocator.TempJob);
            NativeArray<bool> alignToEndMap = new NativeArray<bool>(spline.followerUpdateList.Count, Allocator.TempJob);
            NativeArray<bool> mirrorMap = new NativeArray<bool>(0, Allocator.TempJob);
            localSpaces.Clear();

            for (int i = 0; i < spline.followerUpdateList.Count; i++)
            {
                SplineObject so = spline.followerUpdateList[i];
                points[i] = so.localSplinePosition;

                if (so.lockPosition && so.soParent != null)
                    points[i] = Vector3.zero;

                int combinedParentHashCodes = SplineObjectUtility.GetCombinedParentHashCodes(so);
                if (!localSpaces.ContainsKey(combinedParentHashCodes)) localSpaces.Add(combinedParentHashCodes, SplineObjectUtility.GetCombinedParentMatrixs(so.soParent));
                verticesMap[i] = combinedParentHashCodes;
                alignToEndMap[i] = so.alignToEnd;
            }

            DeformJob deformJob = CreateDeformJob(spline, points, spline.nativeSegmentsLocal, SplineUtilityNative.ToNativeHashMap(localSpaces), verticesMap, mirrorMap, alignToEndMap, SplineObject.Type.FOLLOWER);
            JobHandle jobHandle = deformJob.Schedule(spline.followerUpdateList.Count, 1);
            jobHandle.Complete();

            for (int i = 0; i < spline.followerUpdateList.Count; i++)
            {
                SplineObject so = spline.followerUpdateList[i];

                if (so.lockPosition)
                {
                    //deformJob.vertices[i] can be NaN in some rear cases in the editor. This fixes that.
                    Vector3 newPosition = Vector3.zero;
                    newPosition += deformJob.vertices[i];
                    newPosition += deformJob.forwardDir[i] * so.localSplinePosition.z;
                    newPosition += deformJob.upDir[i] * so.localSplinePosition.y;
                    newPosition += deformJob.rightDir[i] * so.localSplinePosition.x;

                    so.transform.localPosition = newPosition;

                    float fixedTime = spline.TimeToFixedTime(so.splinePosition.z / spline.length);
                    spline.GetNormalsNonAlloc(normals, fixedTime);
                    Quaternion parentRotations = SplineObjectUtility.GetCombinedParentRotations(so.soParent);
                    so.transform.rotation = Quaternion.LookRotation(normals[2], normals[1]) *
                                            so.localSplineRotation;
                }
                else
                {
                    so.transform.localPosition = deformJob.vertices[i];
                    int axels = so.followAxels.x + so.followAxels.y + so.followAxels.z;

                    if (axels != 0)
                    {
                        Quaternion localSplineRotation = Quaternion.LookRotation(deformJob.forwardDir[i], deformJob.upDir[i]);

                        //Set new local rotation. Order is relevant!
                        Quaternion parentRotations = SplineObjectUtility.GetCombinedParentRotations(so.soParent);
                        Quaternion newLocalRotation = Quaternion.Inverse(parentRotations) *         //1. Remove rotation from all so parents.
                                                      localSplineRotation *                         //2. Add forward direction converted to rotation from Spline.
                                                      (parentRotations * so.localSplineRotation);   //3. Add current parentRotations + localSplineRotation in that order!

                        //Better performence when following all 3 axels. Around 10%.
                        if (axels == 3)
                            so.transform.localEulerAngles = newLocalRotation.eulerAngles;
                        else
                        {
                            //Save old rotation euler
                            Vector3 euler = so.transform.localEulerAngles;

                            //Set world space rotation or splineSpace rotation
                            if (so.followAxels.x == 1)
                                euler.x = newLocalRotation.eulerAngles.x;
                            if (so.followAxels.y == 1)
                                euler.y = newLocalRotation.eulerAngles.y;
                            if (so.followAxels.z == 1)
                                euler.z = newLocalRotation.eulerAngles.z;

                            so.transform.localEulerAngles = euler;
                        }
                    }
                }
            }

            DisposeDeformJob(deformJob);
            spline.followerUpdateList.Clear();
        }

        /// <summary>
        /// Complete deformation job.
        /// </summary>
        public static void DisposeDeformJob(DeformJob deformJob)
        {
            deformJob.vertices.Dispose();
            deformJob.meshNormals.Dispose();
            deformJob.meshTangents.Dispose();
            deformJob.localSpaces.Dispose();
            deformJob.verticesMap.Dispose();
            deformJob.mirrorMap.Dispose();
            deformJob.snapDatas.Dispose();
            deformJob.forwardDir.Dispose();
            deformJob.upDir.Dispose();
            deformJob.rightDir.Dispose();
            deformJob.alignToEndMap.Dispose();
            deformJob.soNormalTypeMap.Dispose();

#if UNITY_EDITOR
            EHandleEvents.InvokeDisposeDeformJob();
#endif
        }

        /// <summary>
        /// Creates a deform job for mesh deformations.
        /// </summary>
        /// <returns>The created deform job instance.</returns>
        public static DeformJob CreateDeformJob(Spline spline,
                                                NativeArray<Vector3> vertices,
                                                NativeArray<Vector3> meshNormals,
                                                NativeArray<Vector4> meshTangents,
                                                NativeArray<NativeSegment> nativeSegments,
                                                NativeHashMap<int, float4x4> localSpaces,
                                                NativeArray<int> verticesMap,
                                                NativeArray<bool> mirrorMap,
                                                SplineObject.Type deformationType,
                                                NativeArray<SplineObject.NormalType> soNormalTypeMap,
                                                NativeArray<bool> alignToEndMap,
                                                NativeArray<SnapData> snapDatas)
        {
            DeformJob deformJob = new DeformJob()
            {
                vertices = vertices,
                meshNormals = meshNormals,
                meshTangents = meshTangents,
                forwardDir = new NativeArray<Vector3>(vertices.Length, Allocator.TempJob),
                upDir = new NativeArray<Vector3>(vertices.Length, Allocator.TempJob),
                rightDir = new NativeArray<Vector3>(vertices.Length, Allocator.TempJob),
                localSpaces = localSpaces,
                verticesMap = verticesMap,
                soNormalTypeMap = soNormalTypeMap,
                alignToEndMap = alignToEndMap,
                mirrorMap = mirrorMap,
                splineUpDirection = spline.normalType == Spline.NormalType.STATIC_2D ? -Vector3.forward : Vector2.up,
                nativeSegments = nativeSegments,
                noises = spline.nativeNoises,
                splineLength = spline.length,
                distanceMap = spline.distanceMap,
                normalsArray = spline.normalsLocal,
                positionMap = spline.positionMapLocal,
                splineResolution = spline.GetResolutionSpline(),
                loop = spline.loop,
                normalType = spline.normalType,
                deformationType = deformationType,
                snapDatas = snapDatas
            };

            return deformJob;
        }

        public static DeformJob CreateDeformJob(Spline spline,
                                        NativeArray<Vector3> vertices,
                                        NativeArray<NativeSegment> nativeSegments,
                                        NativeHashMap<int, float4x4> localSpaces,
                                        NativeArray<int> verticesMap,
                                        NativeArray<bool> mirrorMap,
                                        SplineObject.Type deformationType)
        {
            return CreateDeformJob(spline, 
                                   vertices,
                                   new NativeArray<Vector3>(0, Allocator.TempJob),
                                   new NativeArray<Vector4>(0, Allocator.TempJob),
                                   nativeSegments, 
                                   localSpaces, 
                                   verticesMap, 
                                   mirrorMap, 
                                   deformationType,
                                   new NativeArray<SplineObject.NormalType>(0, Allocator.TempJob),
                                   new NativeArray<bool>(0, Allocator.TempJob), 
                                   new NativeArray<SnapData>(0, Allocator.TempJob));
        }

        public static DeformJob CreateDeformJob(Spline spline,
                                NativeArray<Vector3> vertices,
                                NativeArray<NativeSegment> nativeSegments,
                                NativeHashMap<int, float4x4> localSpaces,
                                NativeArray<int> verticesMap,
                                NativeArray<bool> mirrorMap,
                                NativeArray<bool> alignToEndMap,
                                SplineObject.Type deformationType)
        {
            return CreateDeformJob(spline, 
                                   vertices,
                                   new NativeArray<Vector3>(0, Allocator.TempJob),
                                   new NativeArray<Vector4>(0, Allocator.TempJob),
                                   nativeSegments, 
                                   localSpaces, 
                                   verticesMap, 
                                   mirrorMap, 
                                   deformationType,
                                   new NativeArray<SplineObject.NormalType>(0, Allocator.TempJob),
                                   alignToEndMap, 
                                   new NativeArray<SnapData>(0, Allocator.TempJob));
        }

        public static void ClearWorkers()
        {
            deformationWorkers.Clear();
        }

        public static void Deform(SplineObject so, DeformationWorker.Type type, Spline spline)
        {
            DeformationWorker dw = GetWorkerFromPool(type, spline);
            dw.Add(so, spline);
        }

        public static void GetActiveWorkers(DeformationWorker.Type type, List<DeformationWorker> activeWorkersList)
        {
            activeWorkersList.Clear();

            foreach (DeformationWorker dw in deformationWorkers)
            {
                if (dw.state == DeformationWorker.State.IDLE)
                    continue;

                if (dw.type != type)
                    continue;

                activeWorkersList.Add(dw);
            }
        }

        private static DeformationWorker GetWorkerFromPool(DeformationWorker.Type type, Spline spline)
        {
            if (deformationWorkers.Count > 500)
                Debug.LogWarning($"[Spline Architect] Currently: {deformationWorkers.Count} deformation workers exists!");

            foreach (DeformationWorker dw in deformationWorkers)
            {
                if (dw.type != type && dw.type != DeformationWorker.Type.NONE)
                    continue;

                if (dw.spline != spline && dw.spline != null)
                    continue;

                if (dw.state != DeformationWorker.State.IDLE && dw.state != DeformationWorker.State.READY)
                    continue;

                dw.type = type;
                return dw;
            }

            DeformationWorker newDw = new DeformationWorker(type);
            deformationWorkers.Add(newDw);

            return newDw;
        }
    }
}
