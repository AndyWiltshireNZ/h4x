// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: DeformationWorker.cs
//
// Author: Mikael Danielsson
// Date Created: 02-07-2024
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;

using SplineArchitect.Utility;
using SplineArchitect.Jobs;

namespace SplineArchitect.Objects
{
    public class DeformationWorker
    {
        public enum Type
        {
            NONE,
            EDITOR,
            RUNETIME
        }

        public enum State
        {
            IDLE,
            READY,
            READY_AND_FULL,
            WORKING
        }

        //General public
        public Type type;
        public State state { get; private set; }
        public Spline spline { get; private set; }

        //General private
        private float splineLength;

        //Job
        private JobHandle jobHandle;
        private DeformJob deformJob;

        //Lists
        private List<SplineObject> splineObjects = new List<SplineObject>();
        private int totalVertices;

        public DeformationWorker(Type type)
        {
            state = State.IDLE;
            this.type = type;
        }

        public void Add(SplineObject so, Spline spline)
        {
            if (so.meshContainers.Count == 0)
            {
                Debug.LogWarning($"[Spline Architect] Tried to add SplineObject {so.name} with empty meshContainers list.");
                return;
            }

            if (so.splineParent == null)
            {
                Debug.LogWarning($"[Spline Architect] Tried to add SplineObject {so.name} with null splineParent.");
                return;
            }

            int vertices = 0;
            Mesh meshFilterMesh = so.meshContainers.Count > 0 ? so.meshContainers[0].GetInstanceMesh() : null;

            for (int i = 0; i < so.meshContainers.Count; i++)
            {
                MeshContainer mc = so.meshContainers[i];
                Mesh instanceMesh = mc.GetInstanceMesh();

                if (instanceMesh == null)
                    continue;

                //If mesh colliders uses the same mesh as the mesh filter, skip.
                if (i > 0 && meshFilterMesh != null && meshFilterMesh == instanceMesh)
                    continue;

                vertices += instanceMesh.vertexCount;
            }

            if(vertices == 0)
                return;

            totalVertices += vertices;
            splineObjects.Add(so);

            if (this.spline == null)
                this.spline = spline;

            if (state == State.IDLE)
            {
                state = State.READY;
                splineLength = spline.length;
            }

            if (totalVertices > 25000)
                state = State.READY_AND_FULL;
        }

        public void Start()
        {
            if (spline == null)
            {
                Reset();
                return;
            }

            NativeArray<Vector3> vertices = new NativeArray<Vector3>(totalVertices, Allocator.TempJob);
            NativeArray<Vector3> meshNormals = new NativeArray<Vector3>(totalVertices, Allocator.TempJob);
            NativeArray<Vector4> meshTangents = new NativeArray<Vector4>(totalVertices, Allocator.TempJob);
            NativeHashMap<int, float4x4> localSpaces = new NativeHashMap<int, float4x4>(splineObjects.Count, Allocator.TempJob);
            NativeArray<int> verticesMap = new NativeArray<int>(splineObjects.Count, Allocator.TempJob);
            NativeArray<bool> mirrorMap = new NativeArray<bool>(splineObjects.Count, Allocator.TempJob);
            NativeArray<SplineObject.NormalType> soNormalTypeMap = new NativeArray<SplineObject.NormalType>(splineObjects.Count, Allocator.TempJob);
            NativeArray<bool> alignToEndMap = new NativeArray<bool>(splineObjects.Count, Allocator.TempJob);
            NativeArray<SnapData> snapDatas = new NativeArray<SnapData>(splineObjects.Count, Allocator.TempJob);
            int offset = 0;

            //Set deform data
            //SplineObjects
            for (int i = 0; i < splineObjects.Count; i++)
            {
                SplineObject so = splineObjects[i];

                if(so == null)
                {
                    Reset();
                    return;
                }

                float4x4 matrix = SplineObjectUtility.GetCombinedParentMatrixs(so);
                localSpaces.Add(i, matrix);
                Mesh meshFilterMesh = so.meshContainers.Count > 0 ? so.meshContainers[0].GetInstanceMesh() : null;

                //MeshContainers
                for (int i2 = 0; i2 < so.meshContainers.Count; i2++)
                {
                    MeshContainer mc = so.meshContainers[i2];

                    if (i2 > 0 && meshFilterMesh != null && meshFilterMesh == mc.GetInstanceMesh())
                        continue;

                    Vector3[] originVertices = HandleCachedResources.FetchOriginVertices(mc);
                    NativeArray<Vector3>.Copy(originVertices, 0, vertices, offset, originVertices.Length);

                    Vector3[] originNormals = HandleCachedResources.FetchOriginNormals(mc);
                    NativeArray<Vector3>.Copy(originNormals, 0, meshNormals, offset, originNormals.Length);

                    Vector4[] originTangents = HandleCachedResources.FetchOriginTangents(mc);
                    NativeArray<Vector4>.Copy(originTangents, 0, meshTangents, offset, originTangents.Length);

                    offset += originVertices.Length;
                }

                mirrorMap[i] = so.mirrorDeformation;
                alignToEndMap[i] = so.alignToEnd;
                soNormalTypeMap[i] = so.normalType;
                verticesMap[i] = offset;

                if(so.snapMode != SplineObject.SnapMode.NONE) snapDatas[i] = so.CalculateSnapData();
            }

            deformJob = HandleDeformation.CreateDeformJob(spline,
                                                          vertices,
                                                          meshNormals,
                                                          meshTangents,
                                                          spline.nativeSegmentsLocal, 
                                                          localSpaces, 
                                                          verticesMap, 
                                                          mirrorMap, 
                                                          SplineObject.Type.DEFORMATION,
                                                          soNormalTypeMap,
                                                          alignToEndMap, 
                                                          snapDatas);

            jobHandle = deformJob.Schedule(deformJob.vertices.Length, 1);
            state = State.WORKING;
        }

        public void Complete()
        {
            jobHandle.Complete();

            int verticesId = 0;

            for (int i = 0; i < splineObjects.Count; i++)
            {
                SplineObject so = splineObjects[i];

#if UNITY_EDITOR
                if (so == null)
                    continue;

                Vector3 combinedScale = SplineObjectUtility.GetCombinedParentScales(so);
                if (GeneralUtility.IsZero(combinedScale.x) ||
                    GeneralUtility.IsZero(combinedScale.y) ||
                    GeneralUtility.IsZero(combinedScale.z))
                    continue;
#endif

                so.transform.localPosition = so.localSplinePosition;
                so.transform.localRotation = so.localSplineRotation;
                so.monitor.UpdateSplineLength(splineLength);

                Mesh meshFilterMesh = so.meshContainers.Count > 0 ? so.meshContainers[0].GetInstanceMesh() : null;

                for (int i2 = 0; i2 < so.meshContainers.Count; i2++)
                {
                    MeshContainer mc = so.meshContainers[i2];
                    Mesh mesh = mc.GetInstanceMesh();

#if UNITY_EDITOR
                    if (mesh == null)
                        continue;
#endif
                    if (i2 > 0 && meshFilterMesh != null && meshFilterMesh == mesh)
                        continue;

                    mesh.MarkDynamic();
                    Vector3[] container = HandleCachedResources.FetchVerticeNormalContainer(mc);

                    //Vertices
                    NativeArray<Vector3>.Copy(deformJob.vertices, verticesId, container, 0, container.Length);
                    mesh.SetVertices(container);
                    //Bounds
                    mesh.RecalculateBounds();

                    //Normals
                    if ((int)so.normalType < 2)
                    {
                        NativeArray<Vector3>.Copy(deformJob.meshNormals, verticesId, container, 0, container.Length);
                        mesh.SetNormals(container);

                        Vector4[] tangentContainer = HandleCachedResources.FetchOriginTangentsContainer(mc);
                        NativeArray<Vector4>.Copy(deformJob.meshTangents, verticesId, tangentContainer, 0, tangentContainer.Length);
                        mesh.SetTangents(tangentContainer);
                    }
                    else if(so.normalType == SplineObject.NormalType.UNITY_CALCULATED || so.normalType == SplineObject.NormalType.UNITY_CALCULATED_SEAMLESS)
                    {
                        mesh.RecalculateNormals();
                        mesh.RecalculateTangents();
                    }

                    //Updated colliders using the same mesh as meshFilter.
                    if (mc.IsMeshFilter())
                    {
                        foreach (MeshContainer mc2 in so.meshContainers)
                        {
                            Mesh instanceMesh = mc2.GetInstanceMesh();
                            if (instanceMesh == null) continue;
                            //Need to set mesh like this else MeshColliders will not update properly. MeshFilter will work fine without this.
                            if (instanceMesh == mesh) mc2.SetInstanceMesh(mesh);
                        }
                    }
                    else
                    {
                        mc.SetInstanceMesh(mesh);
                    }

                    verticesId += container.Length;
                }

                so.UpdateExternalComponents();
            }

            HandleDeformation.DisposeDeformJob(deformJob);
            Reset();
        }

        public bool IsCompleted()
        {
            return jobHandle.IsCompleted;
        }

        private void Reset()
        {
            //Clear lists
            splineObjects.Clear();

            //Reset data
            type = Type.NONE;
            spline = null;
            state = State.IDLE;
            splineLength = 0;
            totalVertices = 0;
        }
    }
}
